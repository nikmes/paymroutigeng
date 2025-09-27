using System.Linq;
using RoutingEngine.Tests.Infrastructure;
using Xunit;

namespace RoutingEngine.Tests;

public sealed class RuleEvaluationSpec
{
    [Fact]
    public async Task All_operator_requires_all_conditions_to_match()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-OUT-GR-EUR",
                RuleDescription = "Send outbound EUR payments to Greece via Deutsche Bank",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 100,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR",
                CounterpartyBankCountryCode = "GR"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR",
            counterparty: c => c.WithBankCountryCode("GR"));

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Single(result.GreenRoutes);
        Assert.Equal("RULE-OUT-GR-EUR", result.GreenRoutes[0].RuleCode);
    }

    [Fact]
    public async Task Any_operator_matches_when_any_condition_is_true()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-OUT-GR-ANY",
                RuleDescription = "Route when either direction or currency aligns",
                OutcomePolicy = "PassOnMatch",
                Operator = "ANY",
                PriorityWeight = 50,
                CorrBankBic = "NDEASESSXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "USD"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Single(result.GreenRoutes);
        Assert.Equal("RULE-OUT-GR-ANY", result.GreenRoutes[0].RuleCode);
    }

    [Fact]
    public async Task None_operator_only_matches_when_no_predicates_match()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-NONE-CATCH",
                RuleDescription = "Fallback when no customer data overlaps",
                OutcomePolicy = "PassOnMatch",
                Operator = "NONE",
                PriorityWeight = 10,
                CorrBankBic = "CHASUS33XXX",
                CustomerIndustry = "I999",
                PaymentCurrency = "USD"
            });

        var request = RoutingRequestFactory.Create(
            direction: "IN",
            currency: "EUR",
            customer: c => c.WithIndustry("I123"));

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Single(result.GreenRoutes);
        Assert.Equal("RULE-NONE-CATCH", result.GreenRoutes[0].RuleCode);
    }

    [Fact]
    public async Task One_operator_requires_exactly_one_condition_match()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-ONE-TOUCH",
                RuleDescription = "Exactly one predicate must match",
                OutcomePolicy = "PassOnMatch",
                Operator = "ONE",
                PriorityWeight = 70,
                CorrBankBic = "BOFAUS3NXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "USD"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Single(result.GreenRoutes);
        Assert.Equal("BOFAUS3NXXX", result.GreenRoutes[0].CorrBankBic);
    }

    [Fact]
    public async Task Higher_priority_weight_rules_are_returned_first()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-LOW",
                RuleDescription = "Lower priority",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 10,
                CorrBankBic = "LOWBICXXX",
                PaymentDirection = "OUT"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-HIGH",
                RuleDescription = "Higher priority",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 200,
                CorrBankBic = "HIGHBICXXX",
                PaymentDirection = "OUT"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "USD");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal(new[] { "RULE-HIGH", "RULE-LOW" }, result.GreenRoutes.Select(r => r.RuleCode).ToArray());
    }

    [Fact]
    public async Task Red_routes_suppress_green_routes_with_same_corridor()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN",
                RuleDescription = "Primary green route",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 100,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-RED",
                RuleDescription = "Block corridor",
                OutcomePolicy = "FailOnMatch",
                Operator = "ALL",
                PriorityWeight = 150,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Empty(result.GreenRoutes);
        Assert.Single(result.RedRoutes);
        Assert.Equal("RULE-RED", result.RedRoutes[0].RuleCode);
        Assert.Equal("CAN_NOT_ROUTE", result.Decision);
    }

    [Fact]
    public async Task Customer_specific_block_moves_corridor_to_red_list()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-GENERAL",
                RuleDescription = "General pass-through corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 200,
                CorrBankBic = "PASSBIC001",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-RED-CUSTOMER",
                RuleDescription = "Block customer 10001",
                OutcomePolicy = "FailOnMatch",
                Operator = "ALL",
                PriorityWeight = 300,
                CorrBankBic = "PASSBIC001",
                PaymentDirection = "OUT",
                CustomerId = "10001"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-ALT",
                RuleDescription = "Alternative corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 150,
                CorrBankBic = "TFIMCY2NXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR",
            customer: c => c.WithId("10001"));

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);

        var green = Assert.Single(result.GreenRoutes);
        Assert.Equal("RULE-GREEN-ALT", green.RuleCode);
        Assert.Equal("TFIMCY2NXXX", green.CorrBankBic);

        var red = Assert.Single(result.RedRoutes);
        Assert.Equal("RULE-RED-CUSTOMER", red.RuleCode);
        Assert.Equal("PASSBIC001", red.CorrBankBic);
    }

    [Fact]
    public async Task Counterparty_type_predicate_routes_when_type_matches()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-CPTY-BUSINESS",
                RuleDescription = "Route only for business counterparties",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 120,
                CorrBankBic = "BNPAFRPPXXX",
                CounterpartyType = "BUSINESS"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR",
            counterparty: c => c.WithType("BUSINESS"));

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        var route = Assert.Single(result.GreenRoutes);
        Assert.Equal("RULE-CPTY-BUSINESS", route.RuleCode);
        Assert.Equal("BNPAFRPPXXX", route.CorrBankBic);
    }

    [Fact]
    public async Task All_green_corridors_are_returned_when_every_rule_passes()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-001",
                RuleDescription = "Primary euro pass corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 500,
                CorrBankBic = "PASSBIC001",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-002",
                RuleDescription = "Secondary euro pass corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 400,
                CorrBankBic = "PASSBIC002",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-003",
                RuleDescription = "Tertiary euro pass corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 300,
                CorrBankBic = "PASSBIC003",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-004",
                RuleDescription = "Fourth euro pass corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 200,
                CorrBankBic = "PASSBIC004",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-005",
                RuleDescription = "Fifth euro pass corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 100,
                CorrBankBic = "PASSBIC005",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Equal(new[]
        {
            "RULE-GREEN-001",
            "RULE-GREEN-002",
            "RULE-GREEN-003",
            "RULE-GREEN-004",
            "RULE-GREEN-005"
        }, result.GreenRoutes.Select(r => r.RuleCode).ToArray());

        Assert.Empty(result.RedRoutes);
    }
}
