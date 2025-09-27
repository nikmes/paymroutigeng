using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using RoutingEngine.Domain;
using RoutingEngine.Evaluation;

namespace RoutingEngine.Benchmarks;

/// <summary>
/// Baseline benchmark scenarios exercising the routing engine with synthetic rule catalogs.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BaselineBenchmarks
{
    private RoutingEngine.Evaluation.RoutingEngine? _engine;
    private RoutingContext? _context;

    [Params(10, 100, 1000)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rules = GenerateRuleCatalog(RuleCount);
        _engine = new RoutingEngine.Evaluation.RoutingEngine(rules);
        _context = CreateContext();
    }

    [Benchmark]
    public RoutingEvaluationResult EvaluateRouting()
    {
        return _engine!.Evaluate(_context!);
    }

    private static RoutingContext CreateContext()
    {
        var payment = new PaymentContext(PaymentDirection.Out, "EUR");
    var counterparty = new CounterpartyContext("US", "ACMEUS33XXX", null, "ACME OFFSHORE", CounterpartyType.Business);
    var customer = new CustomerContext("CUST-042", "FINTECH", CustomerType.Corporate, "DE44500105175407324931");
        return new RoutingContext(payment, counterparty, customer);
    }

    private static IReadOnlyList<RoutingRule> GenerateRuleCatalog(int count)
    {
        var rules = new List<RoutingRule>(count);

        for (var index = 0; index < count; index++)
        {
            var rule = new RoutingRule
            {
                RuleCodeName = $"RULE-{index:0000}",
                RuleDescription = $"Synthetic routing rule #{index}",
                OutcomePolicy = index % 6 == 0 ? OutcomePolicy.FailOnMatch : OutcomePolicy.PassOnMatch,
                Operator = index % 2 == 0 ? RuleOperator.All : RuleOperator.Any,
                PriorityWeight = count - index,
                CorrBankBic = $"CIBK{index:0000}XXX",
                RuleStatus = RuleStatus.On,
                PaymentDirection = PaymentDirection.Out,
                PaymentCurrency = index % 3 == 0 ? "EUR" : "USD",
                CustomerType = index % 2 == 0 ? CustomerType.Corporate : CustomerType.Individual,
                CounterpartyBankCountryCode = index % 5 == 0 ? "US" : null,
                CounterpartyBankBic = index % 4 == 0 ? "ACMEUS33XXX" : null,
                CounterpartyAccount = index % 8 == 0 ? $"ACCT-{index:0000}" : null,
                CounterpartyName = index % 7 == 0 ? "ACME OFFSHORE" : null,
                CustomerId = index % 9 == 0 ? $"CUST-{index:0000}" : null,
                CustomerIndustry = index % 5 == 0 ? "FINTECH" : "TRADING"
            };

            rules.Add(rule);
        }

        return rules;
    }
}
