using System.Linq;
using RoutingEngine.Tests.Infrastructure;
using Xunit;

namespace RoutingEngine.Tests;

public sealed class RouteDecisionSpec
{
    [Fact]
    public async Task Decision_is_can_route_when_any_green_route_survives()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-ONLY",
                RuleDescription = "Green route",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 100,
                CorrBankBic = "GREENBICXXX",
                PaymentDirection = "OUT"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_ROUTE", result.Decision);
        Assert.Equal("EVALUATED", result.Status);
        Assert.Single(result.GreenRoutes);
        Assert.Empty(result.RedRoutes);
    }

    [Fact]
    public async Task Decision_is_can_not_route_when_green_routes_are_suppressed()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN",
                RuleDescription = "Candidate green route",
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
                PriorityWeight = 200,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_NOT_ROUTE", result.Decision);
        Assert.Single(result.RedRoutes);
        Assert.Empty(result.GreenRoutes);
    }

    [Fact]
    public async Task Audit_trail_records_all_rules_in_priority_order_with_match_flags()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-HIGH",
                RuleDescription = "High priority mismatch",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 300,
                CorrBankBic = "HIGHBICXXX",
                PaymentCurrency = "USD"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-MID",
                RuleDescription = "Match candidate",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 200,
                CorrBankBic = "MIDBICXXX",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-LOW",
                RuleDescription = "Fallback",
                OutcomePolicy = "FailOnMatch",
                Operator = "ANY",
                PriorityWeight = 50,
                CorrBankBic = "LOWBICXXX",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal(3, result.AuditTrail.Count);
        Assert.Equal(new[] { "RULE-HIGH", "RULE-MID", "RULE-LOW" }, result.AuditTrail.Select(a => a.RuleCode).ToArray());

        var high = result.AuditTrail[0];
        Assert.False(high.Match);
        Assert.Equal("PassOnMatch", high.OutcomePolicy);

        var mid = result.AuditTrail[1];
        Assert.True(mid.Match);
        Assert.Equal("PassOnMatch", mid.OutcomePolicy);

        var low = result.AuditTrail[2];
        Assert.True(low.Match);
        Assert.Equal("FailOnMatch", low.OutcomePolicy);
    }

    [Fact]
    public async Task Decision_is_can_not_route_when_catalog_has_no_rules()
    {
        var catalog = RuleCatalogBuilder.BuildJson();
        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        Assert.Equal("CAN_NOT_ROUTE", result.Decision);
        Assert.Equal("NO_MATCH", result.Status);
        Assert.Empty(result.GreenRoutes);
        Assert.Empty(result.RedRoutes);
        Assert.Empty(result.AuditTrail);
    }
}
