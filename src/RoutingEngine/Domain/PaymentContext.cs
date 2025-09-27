namespace RoutingEngine.Domain;

/// <summary>
/// Captures payment metadata used for rule evaluation.
/// </summary>
/// <param name="Direction">Optional payment direction.</param>
/// <param name="Currency">Optional three-character ISO 4217 currency code.</param>
public sealed record PaymentContext(PaymentDirection? Direction, string? Currency);
