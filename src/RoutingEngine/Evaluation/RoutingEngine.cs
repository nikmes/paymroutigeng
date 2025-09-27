using System.Diagnostics;
using System.Linq;
using RoutingEngine.Domain;
using RoutingEngine.Exceptions;
using RoutingEngine.Logging;
using Serilog;

namespace RoutingEngine.Evaluation;

/// <summary>
/// Core evaluation engine that processes routing rules against a request context.
/// </summary>
public sealed class RoutingEngine
{
    private readonly IReadOnlyList<EvaluatorEntry> _evaluators;
    private readonly ILogger? _logger;

    public RoutingEngine(IEnumerable<RoutingRule> rules, RuleConditionFactory? conditionFactory = null, ILogger? logger = null)
    {
        var ruleSet = RoutingGuards.EnsureRules(rules);
        var factory = conditionFactory ?? new RuleConditionFactory();
        _evaluators = ruleSet
            .Where(rule => rule.RuleStatus == RuleStatus.On)
            .OrderByDescending(rule => rule.PriorityWeight)
            .ThenBy(rule => rule.RuleCodeName, StringComparer.OrdinalIgnoreCase)
            .Select(rule => new EvaluatorEntry(rule, factory.Build(rule)))
            .ToArray();
        _logger = logger;
    }

    public RoutingEvaluationResult Evaluate(RoutingContext context)
    {
        var totalElapsed = Stopwatch.StartNew();
        var validatedContext = RoutingGuards.EnsureContext(context);

        var audit = new List<RuleEvaluationAuditRecord>(_evaluators.Count);
        var greenCandidates = new Dictionary<string, RouteCandidate>(StringComparer.OrdinalIgnoreCase);
        var redCandidates = new List<RouteCandidate>();

        foreach (var entry in _evaluators)
        {
            var stopwatch = Stopwatch.StartNew();
            var matched = entry.Evaluator.Evaluate(validatedContext);
            stopwatch.Stop();

            audit.Add(new RuleEvaluationAuditRecord(
                entry.Rule.RuleCodeName,
                matched,
                entry.Rule.OutcomePolicy,
                entry.Rule.PriorityWeight,
                entry.Rule.CorrBankBic,
                stopwatch.Elapsed));

            if (!matched)
            {
                continue;
            }

            var candidate = new RouteCandidate(
                entry.Rule.RuleCodeName,
                entry.Rule.CorrBankBic,
                entry.Rule.RuleDescription,
                entry.Rule.PriorityWeight,
                entry.Rule.OutcomePolicy);

            switch (entry.Rule.OutcomePolicy)
            {
                case OutcomePolicy.PassOnMatch:
                    if (!greenCandidates.ContainsKey(candidate.CorrBankBic))
                    {
                        greenCandidates.Add(candidate.CorrBankBic, candidate);
                    }
                    break;
                case OutcomePolicy.FailOnMatch:
                    redCandidates.Add(candidate);
                    break;
            }
        }

        var redRoutes = redCandidates
            .OrderByDescending(candidate => candidate.PriorityWeight)
            .ThenBy(candidate => candidate.RuleCode, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.ToRouteOutcome())
            .ToList();

        var suppressedCorridors = new HashSet<string>(redRoutes.Select(route => route.CorrBankBic), StringComparer.OrdinalIgnoreCase);

        var greenRoutes = greenCandidates.Values
            .Where(candidate => !suppressedCorridors.Contains(candidate.CorrBankBic))
            .OrderByDescending(candidate => candidate.PriorityWeight)
            .ThenBy(candidate => candidate.RuleCode, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.ToRouteOutcome())
            .ToList();

        var decision = greenRoutes.Count > 0 ? RoutingDecision.CanRoute : RoutingDecision.CanNotRoute;
        var status = greenRoutes.Count == 0 && redRoutes.Count == 0
            ? RoutingEvaluationStatus.NoMatch
            : RoutingEvaluationStatus.Evaluated;

    var result = new RoutingEvaluationResult(
            status,
            decision,
            greenRoutes,
            redRoutes,
            audit);

        totalElapsed.Stop();
        _logger?.LogRoutingEvaluation(result, totalElapsed.Elapsed);

    return result;
    }

    private readonly record struct EvaluatorEntry(RoutingRule Rule, RuleEvaluator Evaluator);

    private sealed record RouteCandidate(
        string RuleCode,
        string CorrBankBic,
        string Description,
        int PriorityWeight,
        OutcomePolicy OutcomePolicy)
    {
        public RouteOutcome ToRouteOutcome() => new(RuleCode, CorrBankBic, Description);
    }
}
