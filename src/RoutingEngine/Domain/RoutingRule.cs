namespace RoutingEngine.Domain;

/// <summary>
/// Immutable representation of a payment routing rule loaded from the catalog.
/// </summary>
public sealed record RoutingRule
{
    /// <summary>Unique identifier for the rule (e.g., RULE-OUT-GR-EUR).</summary>
    public required string RuleCodeName { get; init; }

    /// <summary>Human-readable summary of the rule purpose.</summary>
    public required string RuleDescription { get; init; }

    /// <summary>Specifies whether a match produces a GREEN route or a blocking RED route.</summary>
    public required OutcomePolicy OutcomePolicy { get; init; }

    /// <summary>Logical operator used to combine generated predicates.</summary>
    public required RuleOperator Operator { get; init; }

    /// <summary>Priority weight used to order rules during evaluation.</summary>
    public required int PriorityWeight { get; init; }

    /// <summary>Correspondent bank BIC emitted for this rule.</summary>
    public required string CorrBankBic { get; init; }

    /// <summary>Indicates whether the rule participates in evaluation.</summary>
    public required RuleStatus RuleStatus { get; init; }

    /// <summary>Counterparty bank country code condition (ISO 3166-1 alpha-2).</summary>
    public string? CounterpartyBankCountryCode { get; init; }

    /// <summary>Counterparty bank BIC condition.</summary>
    public string? CounterpartyBankBic { get; init; }

    /// <summary>Counterparty bank account condition.</summary>
    public string? CounterpartyAccount { get; init; }

    /// <summary>Counterparty name condition.</summary>
    public string? CounterpartyName { get; init; }

    /// <summary>Initiating customer identifier condition.</summary>
    public string? CustomerId { get; init; }

    /// <summary>Initiating customer industry categorisation condition.</summary>
    public string? CustomerIndustry { get; init; }

    /// <summary>Initiating customer type condition.</summary>
    public CustomerType? CustomerType { get; init; }

    /// <summary>Payment direction condition.</summary>
    public PaymentDirection? PaymentDirection { get; init; }

    /// <summary>Payment currency condition (ISO 4217 alpha-3).</summary>
    public string? PaymentCurrency { get; init; }
}
