using RoutingEngine.Domain;

namespace RoutingEngine.Rules;

/// <summary>
/// Immutable snapshot of a rule catalog with a monotonically increasing version.
/// </summary>
public sealed record RuleCatalogSnapshot(
    long Version,
    DateTimeOffset Timestamp,
    IReadOnlyList<RoutingRule> Rules
);
