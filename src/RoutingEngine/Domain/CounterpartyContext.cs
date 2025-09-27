namespace RoutingEngine.Domain;

/// <summary>
/// Represents counterparty metadata supplied for routing evaluation.
/// </summary>
/// <param name="BankCountryCode">ISO 3166-1 alpha-2 country code.</param>
/// <param name="BankBic">Counterparty bank BIC.</param>
/// <param name="Account">Counterparty account reference.</param>
/// <param name="Name">Counterparty name, typically up to 140 characters.</param>
public sealed record CounterpartyContext(string? BankCountryCode, string? BankBic, string? Account, string? Name);
