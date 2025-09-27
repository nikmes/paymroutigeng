using System.Linq;
using RoutingEngine.Domain;

namespace RoutingEngine.Evaluation;

/// <summary>
/// Builds rule predicate delegates based on configured rule fields.
/// </summary>
public sealed class RuleConditionFactory
{
    private readonly IStringNormalizer _normalizer;

    public RuleConditionFactory(IStringNormalizer? normalizer = null)
    {
        _normalizer = normalizer ?? new StringNormalizer();
    }

    public RuleEvaluator Build(RoutingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var predicateList = new List<Func<RoutingContext, bool>>();

        AppendPredicate(predicateList, rule.CounterpartyBankCountryCode, ctx => _normalizer.Equals(ctx.Counterparty.BankCountryCode, rule.CounterpartyBankCountryCode));
        AppendPredicate(predicateList, rule.CounterpartyBankBic, ctx => _normalizer.Equals(ctx.Counterparty.BankBic, rule.CounterpartyBankBic));
        AppendPredicate(predicateList, rule.CounterpartyAccount, ctx => _normalizer.Equals(ctx.Counterparty.Account, rule.CounterpartyAccount));
        AppendPredicate(predicateList, rule.CounterpartyName, ctx => _normalizer.Equals(ctx.Counterparty.Name, rule.CounterpartyName));
        AppendPredicate(predicateList, rule.CustomerId, ctx => _normalizer.Equals(ctx.Customer.Id, rule.CustomerId));
        AppendPredicate(predicateList, rule.CustomerIndustry, ctx => _normalizer.Equals(ctx.Customer.Industry, rule.CustomerIndustry));
    AppendPredicate(predicateList, rule.CustomerAccount, ctx => _normalizer.Equals(ctx.Customer.Account, rule.CustomerAccount));
        AppendPredicate(predicateList, rule.PaymentCurrency, ctx => _normalizer.Equals(ctx.Payment.Currency, rule.PaymentCurrency));

        if (rule.CustomerType is not null)
        {
            predicateList.Add(ctx => ctx.Customer.Type == rule.CustomerType);
        }

        if (rule.PaymentDirection is not null)
        {
            predicateList.Add(ctx => ctx.Payment.Direction == rule.PaymentDirection);
        }

        return new RuleEvaluator(rule, predicateList.ToArray());
    }

    private static void AppendPredicate(ICollection<Func<RoutingContext, bool>> predicates, string? expectedValue, Func<RoutingContext, bool> predicate)
    {
        if (!string.IsNullOrWhiteSpace(expectedValue))
        {
            predicates.Add(predicate);
        }
    }
}

public sealed class RuleEvaluator
{
    private readonly RoutingRule _rule;
    private readonly Func<RoutingContext, bool>[] _predicates;

    public RuleEvaluator(RoutingRule rule, Func<RoutingContext, bool>[] predicates)
    {
        _rule = rule;
        _predicates = predicates;
    }

    public bool Evaluate(RoutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_predicates.Length == 0)
        {
            return false;
        }

        return _rule.Operator switch
        {
            RuleOperator.All => _predicates.All(predicate => predicate(context)),
            RuleOperator.Any => _predicates.Any(predicate => predicate(context)),
            RuleOperator.None => !_predicates.Any(predicate => predicate(context)),
            RuleOperator.One => _predicates.Count(predicate => predicate(context)) == 1,
            _ => false
        };
    }
}

public interface IStringNormalizer
{
    string? Normalize(string? value);
    bool Equals(string? left, string? right);
}

public sealed class StringNormalizer : IStringNormalizer
{
    public string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant();
    }

    public bool Equals(string? left, string? right)
    {
        var lhs = Normalize(left);
        var rhs = Normalize(right);
        return StringComparer.OrdinalIgnoreCase.Equals(lhs, rhs);
    }
}
