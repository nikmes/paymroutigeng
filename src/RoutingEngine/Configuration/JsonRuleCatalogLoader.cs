using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoutingEngine.Domain;
using RoutingEngine.Exceptions;

namespace RoutingEngine.Configuration;

/// <summary>
/// Loads routing rules from JSON and maps them into domain models.
/// </summary>
public sealed class JsonRuleCatalogLoader
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly JsonSerializerOptions _serializerOptions;

    public JsonRuleCatalogLoader(JsonSerializerOptions? serializerOptions = null)
    {
        _serializerOptions = serializerOptions is null
            ? new JsonSerializerOptions(DefaultSerializerOptions)
            : serializerOptions;
    }

    public async Task<IReadOnlyList<RoutingRule>> LoadAsync(Stream jsonStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);

        try
        {
            var dtoList = await JsonSerializer.DeserializeAsync<List<RoutingRuleDto>>(jsonStream, _serializerOptions, cancellationToken)
                .ConfigureAwait(false);
            return Materialise(dtoList);
        }
        catch (JsonException ex)
        {
            throw new RoutingValidationException("Unable to parse routing rule catalog.", new[] { ex.Message }, ex);
        }
    }

    public async Task<IReadOnlyList<RoutingRule>> LoadAsync(string json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false);
        return await LoadAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<RoutingRule> Load(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            var dtoList = JsonSerializer.Deserialize<List<RoutingRuleDto>>(json, _serializerOptions);
            return Materialise(dtoList);
        }
        catch (JsonException ex)
        {
            throw new RoutingValidationException("Unable to parse routing rule catalog.", new[] { ex.Message }, ex);
        }
    }

    private IReadOnlyList<RoutingRule> Materialise(IReadOnlyList<RoutingRuleDto>? dtoList)
    {
        if (dtoList is null || dtoList.Count == 0)
        {
            return Array.Empty<RoutingRule>();
        }

        var rules = new List<RoutingRule>(dtoList.Count);
        var errors = new List<string>();

        for (var index = 0; index < dtoList.Count; index++)
        {
            var dto = dtoList[index];
            if (dto is null)
            {
                errors.Add($"Rule[{index}]: Definition is null.");
                continue;
            }

            var prefix = $"Rule[{index}]";
            var errorCheckpoint = errors.Count;

            var code = Require(dto.RuleCodeName, prefix, "RuleCodeName", errors);
            var description = Require(dto.RuleDescription, prefix, "RuleDescription", errors);
            var corrBankBic = Require(dto.CorrBankBic, prefix, "CorrBankBIC", errors);
            var priorityWeight = RequirePriority(dto.PriorityWeight, prefix, errors);
            var outcomePolicy = ParseOutcomePolicy(dto.OutcomePolicy, prefix, errors);
            var ruleOperator = ParseRuleOperator(dto.Operator, prefix, errors);
            var ruleStatus = ParseRuleStatus(dto.RuleStatus, prefix, errors);
            var customerType = ParseCustomerType(dto.CustomerType, prefix, errors);
            var paymentDirection = ParsePaymentDirection(dto.PaymentDirection, prefix, errors);

            if (errors.Count > errorCheckpoint)
            {
                continue;
            }

            var normalisedCorrBankBic = NormalizeUpper(corrBankBic!) ?? corrBankBic!.ToUpperInvariant();

            rules.Add(new RoutingRule
            {
                RuleCodeName = code!,
                RuleDescription = description!,
                OutcomePolicy = outcomePolicy!.Value,
                Operator = ruleOperator!.Value,
                PriorityWeight = priorityWeight!.Value,
                CorrBankBic = normalisedCorrBankBic,
                RuleStatus = ruleStatus!.Value,
                CounterpartyBankCountryCode = NormalizeUpper(dto.CounterpartyBankCountryCode),
                CounterpartyBankBic = NormalizeUpper(dto.CounterpartyBankBic),
                CounterpartyAccount = NormalizeOptional(dto.CounterpartyAccount),
                CounterpartyName = NormalizeOptional(dto.CounterpartyName),
                CustomerId = NormalizeOptional(dto.CustomerId),
                CustomerIndustry = NormalizeUpper(dto.CustomerIndustry),
                CustomerType = customerType,
                PaymentDirection = paymentDirection,
                PaymentCurrency = NormalizeUpper(dto.PaymentCurrency)
            });
        }

        if (errors.Count > 0)
        {
            throw new RoutingValidationException("Rule catalog validation failed.", errors);
        }

        return rules;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeUpper(string? value)
    {
        var normalised = NormalizeOptional(value);
        return normalised?.ToUpperInvariant();
    }

    private static string? Require(string? value, string prefix, string fieldName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{prefix}: {fieldName} is required.");
            return null;
        }

        return value.Trim();
    }

    private static int? RequirePriority(int? value, string prefix, ICollection<string> errors)
    {
        if (value is null)
        {
            errors.Add($"{prefix}: PriorityWeight is required.");
            return null;
        }

        if (value < 0)
        {
            errors.Add($"{prefix}: PriorityWeight must be zero or a positive number.");
            return null;
        }

        return value;
    }

    private static OutcomePolicy? ParseOutcomePolicy(string? value, string prefix, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{prefix}: OutcomePolicy is required.");
            return null;
        }

        return value.Trim() switch
        {
            "PassOnMatch" => OutcomePolicy.PassOnMatch,
            "FailOnMatch" => OutcomePolicy.FailOnMatch,
            _ => AddError<OutcomePolicy?>(errors, prefix, "OutcomePolicy", value)
        };
    }

    private static RuleOperator? ParseRuleOperator(string? value, string prefix, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{prefix}: Operator is required.");
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "ALL" => RuleOperator.All,
            "ANY" => RuleOperator.Any,
            "NONE" => RuleOperator.None,
            "ONE" => RuleOperator.One,
            _ => AddError<RuleOperator?>(errors, prefix, "Operator", value)
        };
    }

    private static RuleStatus? ParseRuleStatus(string? value, string prefix, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RuleStatus.On;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "ON" => RuleStatus.On,
            "OFF" => RuleStatus.Off,
            _ => AddError<RuleStatus?>(errors, prefix, "RuleStatus", value)
        };
    }

    private static CustomerType? ParseCustomerType(string? value, string prefix, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "INDIVIDUAL" => CustomerType.Individual,
            "CORPORATE" or "COORPORATE" => CustomerType.Corporate,
            _ => AddError<CustomerType?>(errors, prefix, "PR.CustomerType", value)
        };
    }

    private static PaymentDirection? ParsePaymentDirection(string? value, string prefix, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "IN" => PaymentDirection.In,
            "OUT" => PaymentDirection.Out,
            "INT" => PaymentDirection.International,
            "OWN" => PaymentDirection.Own,
            _ => AddError<PaymentDirection?>(errors, prefix, "PR.PaymentDirection", value)
        };
    }

    private static T? AddError<T>(ICollection<string> errors, string prefix, string fieldName, string? value)
    {
        errors.Add($"{prefix}: {fieldName} value '{value}' is invalid.");
        return default;
    }

    private sealed record RoutingRuleDto
    {
        public string? RuleCodeName { get; init; }
        public string? RuleDescription { get; init; }
        public string? OutcomePolicy { get; init; }
        public string? Operator { get; init; }
        public string? CorrBankBic { get; init; }
        public int? PriorityWeight { get; init; }
        public string? RuleStatus { get; init; }

        [JsonPropertyName("PR.CPartyBankCountryCode")]
        public string? CounterpartyBankCountryCode { get; init; }

        [JsonPropertyName("PR.CPartyBankBIC")]
        public string? CounterpartyBankBic { get; init; }

        [JsonPropertyName("PR.CPartyAccount")]
        public string? CounterpartyAccount { get; init; }

        [JsonPropertyName("PR.CPartyName")]
        public string? CounterpartyName { get; init; }

        [JsonPropertyName("PR.CustomerId")]
        public string? CustomerId { get; init; }

        [JsonPropertyName("PR.CustomerIndustry")]
        public string? CustomerIndustry { get; init; }

        [JsonPropertyName("PR.CustomerType")]
        public string? CustomerType { get; init; }

        [JsonPropertyName("PR.PaymentDirection")]
        public string? PaymentDirection { get; init; }

        [JsonPropertyName("PR.PaymentCurrency")]
        public string? PaymentCurrency { get; init; }
    }
}
