namespace RoutingEngine.Domain;

/// <summary>
/// Aggregates all contextual data provided for a routing evaluation request.
/// </summary>
/// <param name="Payment">Payment-level qualifiers.</param>
/// <param name="Counterparty">Counterparty metadata.</param>
/// <param name="Customer">Customer metadata.</param>
public sealed record RoutingContext(PaymentContext Payment, CounterpartyContext Counterparty, CustomerContext Customer);
