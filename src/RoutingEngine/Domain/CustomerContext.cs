namespace RoutingEngine.Domain;

/// <summary>
/// Holds customer-specific qualifiers that may influence routing.
/// </summary>
/// <param name="Id">Internal customer identifier.</param>
/// <param name="Industry">Industry code when available.</param>
/// <param name="Type">Categorisation of the customer, e.g., corporate vs individual.</param>
/// <param name="Account">Customer's account identifier (e.g., IBAN or domestic account number) used for predicate matching.</param>
public sealed record CustomerContext(string? Id, string? Industry, CustomerType? Type, string? Account);
