namespace RoutingEngine.Domain;

/// <summary>
/// Represents a GREEN or RED route returned to consumers of the routing engine.
/// </summary>
/// <param name="RuleCode">Identifier of the rule that produced the outcome.</param>
/// <param name="CorrBankBic">Correspondent bank corridor associated with the outcome.</param>
/// <param name="Description">Human-readable explanation of the route.</param>
public sealed record RouteOutcome(string RuleCode, string CorrBankBic, string Description);
