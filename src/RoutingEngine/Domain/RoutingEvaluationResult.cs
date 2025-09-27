namespace RoutingEngine.Domain;

/// <summary>
/// Represents the aggregated result of evaluating a routing request against the catalog.
/// </summary>
/// <param name="Status">Overall evaluation status (evaluated vs no match).</param>
/// <param name="Decision">Derived routing decision based on surviving GREEN routes.</param>
/// <param name="GreenRoutes">GREEN routes that remain eligible.</param>
/// <param name="RedRoutes">RED routes describing blocked corridors.</param>
/// <param name="AuditTrail">Detailed per-rule evaluation diagnostics.</param>
public sealed record RoutingEvaluationResult(
    RoutingEvaluationStatus Status,
    RoutingDecision Decision,
    IReadOnlyList<RouteOutcome> GreenRoutes,
    IReadOnlyList<RouteOutcome> RedRoutes,
    IReadOnlyList<RuleEvaluationAuditRecord> AuditTrail)
{
    /// <summary>
    /// Convenience factory for responses with no matches.
    /// </summary>
    public static RoutingEvaluationResult NoMatch() => new(
        RoutingEvaluationStatus.NoMatch,
        RoutingDecision.CanNotRoute,
        Array.Empty<RouteOutcome>(),
        Array.Empty<RouteOutcome>(),
        Array.Empty<RuleEvaluationAuditRecord>());
}
