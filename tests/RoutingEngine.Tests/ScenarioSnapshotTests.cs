using System.Threading.Tasks;
using RoutingEngine.Tests.Infrastructure;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace RoutingEngine.Tests;

public sealed class ScenarioSnapshotTests
{
    [Fact]
    public async Task Mixed_green_and_red_routes_are_verified()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-PRIMARY",
                RuleDescription = "Primary routing corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 200,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT",
                PaymentCurrency = "EUR"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-GREEN-SECONDARY",
                RuleDescription = "Secondary routing corridor",
                OutcomePolicy = "PassOnMatch",
                Operator = "ANY",
                PriorityWeight = 150,
                CorrBankBic = "CHASUS33XXX",
                PaymentDirection = "OUT"
            },
            new RuleDefinition
            {
                RuleCodeName = "RULE-RED-BLOCK",
                RuleDescription = "Block corridor for sanctioned entity",
                OutcomePolicy = "FailOnMatch",
                Operator = "ALL",
                PriorityWeight = 300,
                CorrBankBic = "DEUTDEFFXXX",
                CounterpartyName = "SANCTIONED ENTITY"
            });

        var request = RoutingRequestFactory.Create(
            direction: "OUT",
            currency: "EUR",
            counterparty: c => c.WithName("SANCTIONED ENTITY"));

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        await Verifier.Verify(result)
            .UseMethodName("Mixed_green_and_red_routes_are_verified")
            .UseDirectory("Snapshots");
    }

    [Fact]
    public async Task No_matching_rules_are_verified()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-NONMATCH",
                RuleDescription = "Does not match",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 50,
                CorrBankBic = "BNPAFRPPXXX",
                PaymentDirection = "IN",
                PaymentCurrency = "USD"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        await Verifier.Verify(result)
            .UseMethodName("No_matching_rules_are_verified")
            .UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Empty_catalog_response_is_verified()
    {
        var catalog = RuleCatalogBuilder.BuildJson();
        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        await Verifier.Verify(result)
            .UseMethodName("Empty_catalog_response_is_verified")
            .UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Customer_specific_block_snapshot_is_verified()
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
                RuleDescription = "Block customer 10001 For Outgoing on PASSBIC001",
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

        await Verifier.Verify(result)
            .UseMethodName("Customer_specific_block_snapshot_is_verified")
            .UseDirectory("Snapshots");
    }
}
