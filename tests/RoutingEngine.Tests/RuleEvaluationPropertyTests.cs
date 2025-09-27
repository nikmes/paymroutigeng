using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using RoutingEngine.Tests.Infrastructure;

namespace RoutingEngine.Tests;

public sealed class RuleEvaluationPropertyTests
{
    [Property(MaxTest = 75)]
    public bool Evaluation_is_deterministic(int seed)
    {
        var scenario = PropertyScenario.FromSeed(seed);
        var first = RoutingEngineTestHarness.EvaluateAsync(scenario.CatalogJson, scenario.Request).GetAwaiter().GetResult();
        var second = RoutingEngineTestHarness.EvaluateAsync(scenario.CatalogJson, scenario.Request).GetAwaiter().GetResult();
        return first == second;
    }

    [Property(MaxTest = 75)]
    public bool Decision_flag_aligns_with_green_routes(int seed)
    {
        var scenario = PropertyScenario.FromSeed(seed);
        var result = RoutingEngineTestHarness.EvaluateAsync(scenario.CatalogJson, scenario.Request).GetAwaiter().GetResult();
        return result.Decision == "CAN_ROUTE" ? result.GreenRoutes.Any() : !result.GreenRoutes.Any();
    }

    [Property(MaxTest = 75)]
    public bool Corridors_do_not_exist_in_both_lists(int seed)
    {
        var scenario = PropertyScenario.FromSeed(seed);
        var result = RoutingEngineTestHarness.EvaluateAsync(scenario.CatalogJson, scenario.Request).GetAwaiter().GetResult();
        var green = result.GreenRoutes.Select(r => r.CorrBankBic).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return result.RedRoutes.All(r => !green.Contains(r.CorrBankBic));
    }

    private sealed record PropertyScenario(string CatalogJson, RoutingRequestDto Request)
    {
        public static PropertyScenario FromSeed(int seed)
        {
            var generator = new ScenarioGenerator(seed);
            return new PropertyScenario(generator.BuildCatalogJson(), generator.BuildRequest());
        }
    }

    private sealed class ScenarioGenerator
    {
        private static readonly string[] DirectionPool = { "IN", "OUT", "INT", "OWN" };
        private static readonly string[] BicPool =
        {
            "DEUTDEFFXXX",
            "CHASUS33XXX",
            "BOFAUS3NXXX",
            "CITIUS33XXX",
            "NDEASESSXXX",
            "HSBCGB2LXXX",
            "BARCGB22XXX",
            "BNPAFRPPXXX"
        };

        private static readonly string[] CountryPool = { "GR", "CY", "DE", "GB", "US", "NL" };
        private static readonly string[] CurrencyPool = { "EUR", "USD", "GBP", "CHF", "JPY" };
        private static readonly string[] IndustryPool = { "I001", "I002", "I003", "I004", "I999" };
        private static readonly string[] CustomerTypes = { "INDIVIDUAL", "CORPORATE" };
        private static readonly string[] Operators = { "ALL", "ANY", "NONE", "ONE" };
        private static readonly string[] OutcomePolicies = { "PassOnMatch", "FailOnMatch" };
        private static readonly string[] Descriptions =
        {
            "Auto-generated pass route",
            "Auto-generated contingency route",
            "Auto-generated block route",
            "Sanctions guard"
        };

        private readonly Random _random;
        private readonly List<RuleDefinition> _rules = new();

        internal ScenarioGenerator(int seed)
        {
            _random = new Random(unchecked(seed * 397) ^ 0x5F3759DF);
            GenerateRules();
        }

        internal string BuildCatalogJson() => RuleCatalogBuilder.BuildJson(_rules.ToArray());

        internal RoutingRequestDto BuildRequest()
        {
            var direction = PickFrom(DirectionPool);
            var currency = PickFrom(CurrencyPool);

            var counterparty = new CounterpartyDto(
                MaybeFrom(CountryPool),
                MaybeFrom(BicPool),
                Maybe($"ACCT-{_random.Next(10_000, 999_999)}"),
                Maybe(PickFrom(new[] { "ACME LTD", "FOO BAR", "SANCTIONED ENTITY", "TRUSTED SUPPLIER" })));

            var customer = new CustomerDto(
                Maybe($"CUST-{_random.Next(1, 999):000}"),
                MaybeFrom(IndustryPool),
                MaybeFrom(CustomerTypes),
                Maybe($"DE{_random.Next(10_000_000, 99_999_999)}{_random.Next(10_000_000, 99_999_999)}"));

            return new RoutingRequestDto(
                new PaymentDto(direction, currency),
                counterparty,
                customer);
        }

        private void GenerateRules()
        {
            var ruleCount = _random.Next(2, 7);
            var hasPass = false;
            var hasFail = false;

            for (var index = 0; index < ruleCount; index++)
            {
                var outcome = OutcomePolicies[_random.Next(OutcomePolicies.Length)];
                hasPass |= outcome == "PassOnMatch";
                hasFail |= outcome == "FailOnMatch";

                var definition = new RuleDefinition
                {
                    RuleCodeName = $"RULE-{Math.Abs(_random.Next()) % 10_000:0000}-{index:00}",
                    RuleDescription = PickFrom(Descriptions),
                    OutcomePolicy = outcome,
                    Operator = Operators[_random.Next(Operators.Length)],
                    PriorityWeight = _random.Next(1, 500),
                    CorrBankBic = PickFrom(BicPool),
                    RuleStatus = "ON",
                    CounterpartyBankCountryCode = MaybeFrom(CountryPool),
                    CounterpartyBankBic = MaybeFrom(BicPool),
                    CounterpartyAccount = Maybe($"ACCT-{_random.Next(10_000, 999_999)}"),
                    CounterpartyName = Maybe(PickFrom(new[] { "ACME LTD", "FOO BAR", "SANCTIONED ENTITY" })),
                    CustomerId = Maybe($"CUST-{_random.Next(1, 999):000}"),
                    CustomerIndustry = MaybeFrom(IndustryPool),
                    CustomerType = MaybeFrom(CustomerTypes),
                    PaymentDirection = MaybeFrom(DirectionPool),
                    PaymentCurrency = MaybeFrom(CurrencyPool)
                };

                _rules.Add(definition);
            }

            if (!hasPass)
            {
                _rules[0] = _rules[0] with { OutcomePolicy = "PassOnMatch" };
            }

            if (!hasFail && _rules.Count > 1)
            {
                _rules[^1] = _rules[^1] with { OutcomePolicy = "FailOnMatch" };
            }
        }

        private string PickFrom(IReadOnlyList<string> values) => values[_random.Next(values.Count)];

        private string? MaybeFrom(IReadOnlyList<string> values) => _random.NextDouble() < 0.5 ? PickFrom(values) : null;

        private string? Maybe(string value) => _random.NextDouble() < 0.5 ? value : null;
    }
}
