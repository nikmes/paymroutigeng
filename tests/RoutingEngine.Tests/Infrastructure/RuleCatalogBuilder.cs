using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoutingEngine.Tests.Infrastructure;

internal static class RuleCatalogBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static string BuildJson(params RuleDefinition[] rules)
    {
        return JsonSerializer.Serialize(rules.Select(ToDictionary), SerializerOptions);
    }

    private static IDictionary<string, object?> ToDictionary(RuleDefinition definition)
    {
        var map = new Dictionary<string, object?>
        {
            ["RuleCodeName"] = definition.RuleCodeName,
            ["RuleDescription"] = definition.RuleDescription,
            ["OutcomePolicy"] = definition.OutcomePolicy,
            ["Operator"] = definition.Operator,
            ["CorrBankBIC"] = definition.CorrBankBic,
            ["PriorityWeight"] = definition.PriorityWeight,
            ["RuleStatus"] = definition.RuleStatus
        };

        if (!string.IsNullOrWhiteSpace(definition.CounterpartyBankCountryCode))
        {
            map["PR.CPartyBankCountryCode"] = definition.CounterpartyBankCountryCode;
        }

        if (!string.IsNullOrWhiteSpace(definition.CounterpartyBankBic))
        {
            map["PR.CPartyBankBIC"] = definition.CounterpartyBankBic;
        }

        if (!string.IsNullOrWhiteSpace(definition.CounterpartyAccount))
        {
            map["PR.CPartyAccount"] = definition.CounterpartyAccount;
        }

        if (!string.IsNullOrWhiteSpace(definition.CounterpartyName))
        {
            map["PR.CPartyName"] = definition.CounterpartyName;
        }

        if (!string.IsNullOrWhiteSpace(definition.CounterpartyType))
        {
            map["PR.CPartyType"] = definition.CounterpartyType;
        }

        if (!string.IsNullOrWhiteSpace(definition.CustomerId))
        {
            map["PR.CustomerId"] = definition.CustomerId;
        }

        if (!string.IsNullOrWhiteSpace(definition.CustomerIndustry))
        {
            map["PR.CustomerIndustry"] = definition.CustomerIndustry;
        }

        if (!string.IsNullOrWhiteSpace(definition.CustomerType))
        {
            map["PR.CustomerType"] = definition.CustomerType;
        }

        if (!string.IsNullOrWhiteSpace(definition.CustomerAccount))
        {
            map["PR.CustomerAccount"] = definition.CustomerAccount;
        }

        if (!string.IsNullOrWhiteSpace(definition.PaymentDirection))
        {
            map["PR.PaymentDirection"] = definition.PaymentDirection;
        }

        if (!string.IsNullOrWhiteSpace(definition.PaymentCurrency))
        {
            map["PR.PaymentCurrency"] = definition.PaymentCurrency;
        }

        return map;
    }
}

internal sealed record RuleDefinition
{
    public required string RuleCodeName { get; init; }
    public required string RuleDescription { get; init; }
    public required string OutcomePolicy { get; init; }
    public required string Operator { get; init; }
    public required string CorrBankBic { get; init; }
    public required int PriorityWeight { get; init; }
    public string RuleStatus { get; init; } = "ON";
    public string? CounterpartyBankCountryCode { get; init; }
    public string? CounterpartyBankBic { get; init; }
    public string? CounterpartyAccount { get; init; }
    public string? CounterpartyName { get; init; }
    public string? CounterpartyType { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerIndustry { get; init; }
    public string? CustomerType { get; init; }
    public string? CustomerAccount { get; init; }
    public string? PaymentDirection { get; init; }
    public string? PaymentCurrency { get; init; }
}
