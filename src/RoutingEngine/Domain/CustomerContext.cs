namespace RoutingEngine.Domain;

/// <summary>
/// Holds customer-specific qualifiers that may influence routing.
/// </summary>
/// <param name="Id">Internal customer identifier.</param>
/// <param name="Industry">Industry code when available.</param>
/// <param name="Type">Categorisation of the customer, e.g., corporate vs individual.</param>
public sealed record CustomerContext(string? Id, string? Industry, CustomerType? Type);
