using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.Json.Serialization;
using RoutingEngine.Configuration;
using RoutingEngine.Domain;
using RoutingEngine.Evaluation;

namespace RoutingEngine.Tests.Infrastructure;

internal static class RoutingEngineTestHarness
{
    private static readonly JsonRuleCatalogLoader CatalogLoader = new();

    public static Task<RoutingTestResult> EvaluateAsync(string catalogJson, RoutingRequestDto request)
    {
        return EvaluateAsync(catalogJson, request, CancellationToken.None);
    }

    public static async Task<RoutingTestResult> EvaluateAsync(string catalogJson, RoutingRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalogJson);
        ArgumentNullException.ThrowIfNull(request);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(catalogJson));
        var rules = await CatalogLoader.LoadAsync(stream, cancellationToken).ConfigureAwait(false);

        var engine = new RoutingEngine.Evaluation.RoutingEngine(rules);
        var context = MapRequest(request);
        var evaluation = engine.Evaluate(context);

        return MapResult(evaluation);
    }

    private static RoutingContext MapRequest(RoutingRequestDto request)
    {
        var paymentDirection = ParseEnum<PaymentDirection>(request.Payment.Direction);
        var customerType = ParseEnum<CustomerType>(request.Customer.Type);

        var payment = new PaymentContext(paymentDirection, request.Payment.Currency);
        var counterpartyType = ParseEnum<CounterpartyType>(request.Counterparty.Type);
        var counterparty = new CounterpartyContext(
            request.Counterparty.BankCountryCode,
            request.Counterparty.BankBic,
            request.Counterparty.Account,
            request.Counterparty.Name,
            counterpartyType);
        var customer = new CustomerContext(
            request.Customer.Id,
            request.Customer.Industry,
            customerType,
            request.Customer.Account);

        return new RoutingContext(payment, counterparty, customer);
    }

    // Expose for host tests
    public static RoutingContext GetContext(RoutingRequestDto request) => MapRequest(request);

    private static RoutingTestResult MapResult(RoutingEvaluationResult evaluation)
    {
        var greenRoutes = evaluation.GreenRoutes
            .Select(route => new RouteRecord(route.RuleCode, route.CorrBankBic, route.Description))
            .ToList();

        var redRoutes = evaluation.RedRoutes
            .Select(route => new RouteRecord(route.RuleCode, route.CorrBankBic, route.Description))
            .ToList();

        var auditTrail = evaluation.AuditTrail
            .Select(record => new RuleAuditRecord(
                record.RuleCode,
                record.Matched,
                ToOutcomePolicyString(record.OutcomePolicy)))
            .ToList();

        return new RoutingTestResult(
            ToDecisionString(evaluation.Decision),
            greenRoutes,
            redRoutes,
            auditTrail,
            ToStatusString(evaluation.Status));
    }

    private static string ToDecisionString(RoutingDecision decision) => decision switch
    {
        RoutingDecision.CanRoute => "CAN_ROUTE",
        RoutingDecision.CanNotRoute => "CAN_NOT_ROUTE",
        _ => decision.ToString().ToUpperInvariant()
    };

    private static string ToStatusString(RoutingEvaluationStatus status) => status switch
    {
        RoutingEvaluationStatus.Evaluated => "EVALUATED",
        RoutingEvaluationStatus.NoMatch => "NO_MATCH",
        _ => status.ToString().ToUpperInvariant()
    };

    private static string ToOutcomePolicyString(OutcomePolicy policy) => policy switch
    {
        OutcomePolicy.PassOnMatch => "PassOnMatch",
        OutcomePolicy.FailOnMatch => "FailOnMatch",
        _ => policy.ToString()
    };

    private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
internal sealed class RoutingTestResult : IEquatable<RoutingTestResult>
{
    private static readonly IEqualityComparer<RouteRecord> RouteComparer = EqualityComparer<RouteRecord>.Default;
    private static readonly IEqualityComparer<RuleAuditRecord> AuditComparer = new RuleAuditRecordComparer();

    [JsonPropertyName("decision")]
    public string Decision { get; }

    [JsonPropertyName("greenRoutes")]
    public IReadOnlyList<RouteRecord> GreenRoutes { get; }

    [JsonPropertyName("redRoutes")]
    public IReadOnlyList<RouteRecord> RedRoutes { get; }

    [JsonPropertyName("auditTrail")]
    public IReadOnlyList<RuleAuditRecord> AuditTrail { get; }

    [JsonPropertyName("status")]
    public string Status { get; }

    public RoutingTestResult(
        string decision,
        IReadOnlyList<RouteRecord> greenRoutes,
        IReadOnlyList<RouteRecord> redRoutes,
        IReadOnlyList<RuleAuditRecord> auditTrail,
        string status = "EVALUATED")
    {
        Decision = decision;
        GreenRoutes = greenRoutes ?? Array.Empty<RouteRecord>();
        RedRoutes = redRoutes ?? Array.Empty<RouteRecord>();
        AuditTrail = auditTrail ?? Array.Empty<RuleAuditRecord>();
        Status = status;
    }

    public bool Equals(RoutingTestResult? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return string.Equals(Decision, other.Decision, StringComparison.Ordinal)
            && SequenceEqual(GreenRoutes, other.GreenRoutes, RouteComparer)
            && SequenceEqual(RedRoutes, other.RedRoutes, RouteComparer)
            && SequenceEqual(AuditTrail, other.AuditTrail, AuditComparer)
            && string.Equals(Status, other.Status, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is RoutingTestResult other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Decision, StringComparer.Ordinal);
        AddSequenceHash(ref hash, GreenRoutes, RouteComparer);
        AddSequenceHash(ref hash, RedRoutes, RouteComparer);
        AddSequenceHash(ref hash, AuditTrail, AuditComparer);
        hash.Add(Status, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    public static bool operator ==(RoutingTestResult? left, RoutingTestResult? right)
        => EqualityComparer<RoutingTestResult>.Default.Equals(left, right);

    public static bool operator !=(RoutingTestResult? left, RoutingTestResult? right)
        => !(left == right);

    private static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right, IEqualityComparer<T> comparer)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!comparer.Equals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddSequenceHash<T>(ref HashCode hash, IReadOnlyList<T> source, IEqualityComparer<T> comparer)
    {
        for (var index = 0; index < source.Count; index++)
        {
            hash.Add(source[index], comparer);
        }
    }

    private sealed class RuleAuditRecordComparer : IEqualityComparer<RuleAuditRecord>
    {
        public bool Equals(RuleAuditRecord? x, RuleAuditRecord? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.RuleCode, y.RuleCode, StringComparison.Ordinal)
                && x.Match == y.Match
                && string.Equals(x.OutcomePolicy, y.OutcomePolicy, StringComparison.Ordinal);
        }

        public int GetHashCode(RuleAuditRecord obj)
        {
            var hash = new HashCode();
            hash.Add(obj.RuleCode, StringComparer.Ordinal);
            hash.Add(obj.Match);
            hash.Add(obj.OutcomePolicy, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }
}

internal sealed record RouteRecord(
    [property: JsonPropertyName("ruleCode")] string RuleCode,
    [property: JsonPropertyName("corrBankBic")] string CorrBankBic,
    [property: JsonPropertyName("description")] string Description);

internal sealed record RuleAuditRecord(
    [property: JsonPropertyName("ruleCode")] string RuleCode,
    [property: JsonPropertyName("match")] bool Match,
    [property: JsonPropertyName("outcomePolicy")] string OutcomePolicy);

public sealed record RoutingRequestDto(
    PaymentDto Payment,
    CounterpartyDto Counterparty,
    CustomerDto Customer);

public sealed record PaymentDto(string Direction, string Currency);

public sealed record CounterpartyDto(string? BankCountryCode, string? BankBic, string? Account, string? Name, string? Type);

public sealed record CustomerDto(string? Id, string? Industry, string? Type, string? Account);
