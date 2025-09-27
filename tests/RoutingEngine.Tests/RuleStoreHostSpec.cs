using RoutingEngine.Domain;
using RoutingEngine.Evaluation;
using RoutingEngine.Rules;
using Xunit;
using RoutingEngine.Tests.Infrastructure;
using RoutingEngine.Capabilities;
using System.Collections.Generic;
using RoutingEngine.Configuration;

namespace RoutingEngine.Tests;

public class RuleStoreHostSpec
{
    private static RoutingRule MakeGreen(string code, string bic, int weight = 100) => new()
    {
        RuleCodeName = code,
        RuleDescription = "test",
        OutcomePolicy = OutcomePolicy.PassOnMatch,
    Operator = RuleOperator.All,
        PriorityWeight = weight,
        CorrBankBic = bic,
        RuleStatus = RuleStatus.On,
        PaymentDirection = PaymentDirection.Out,
        PaymentCurrency = "EUR"
    };

    [Fact]
    public async Task Host_reuses_engine_until_version_changes_then_rebuilds()
    {
        var r1 = MakeGreen("R1", "BNPAFRPPXXX");
        var store = new InMemoryRuleStore(new[] { r1 });
        var host = new RoutingEngineHost(store);

        var ctx = new RoutingContext(
            new PaymentContext(PaymentDirection.Out, "EUR"),
            new CounterpartyContext(null, null, null, null, null),
            new CustomerContext(null, null, null, null));

        var res1 = await host.EvaluateAsync(ctx);
        Assert.Equal(RoutingDecision.CanRoute, res1.Decision);
        Assert.Single(res1.GreenRoutes);

        // Update rules
        var r2 = MakeGreen("R2", "CHASUS33XXX", 200);
        await store.ReplaceAllAsync(new[] { r2 });

        var res2 = await host.EvaluateAsync(ctx);
        Assert.Equal("R2", Assert.Single(res2.GreenRoutes).RuleCode);
    }

    [Fact]
    public async Task Post_processor_enriches_green_route_with_nostro_when_supported()
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
                PaymentCurrency = "EUR"
            });

    var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR", chargeBearer: "SHA");

        // Build engine host with in-memory rule store and a fake capabilities store
        var loader = new JsonRuleCatalogLoader();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(catalog));
        var rules = await loader.LoadAsync(stream, CancellationToken.None);
        var snapshot = new RoutingEngine.Rules.RuleCatalogSnapshot(1, DateTimeOffset.UtcNow, rules);
        var ruleStore = new InMemoryRuleStoreStub(snapshot);

        var caps = new Dictionary<string, IReadOnlyDictionary<string, CurrencyCapability>>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEUTDEFFXXX"] = new Dictionary<string, CurrencyCapability>(StringComparer.OrdinalIgnoreCase)
            {
                ["EUR"] = new CurrencyCapability("DE12123456789012345678", new HashSet<string>(StringComparer.OrdinalIgnoreCase){"BEN","SHA","OWN"})
            }
        };
        var capSnapshot = new CorridorCapabilitiesSnapshot(1, DateTimeOffset.UtcNow, caps);
        var capStore = new CapabilitiesStoreStub(capSnapshot);

        var host = new RoutingEngineHost(ruleStore, capStore, new[] { new CapabilityPostProcessor() });
        var context = RoutingEngineTestHarness.GetContext(request);
        var result = await host.EvaluateAsync(context);

        var green = Assert.Single(result.GreenRoutes);
        Assert.Contains("Nostro:", green.Description);
    }

    [Fact]
    public async Task Post_processor_demotes_when_charge_bearer_unsupported()
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
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR", chargeBearer: "BEN");

        var loader = new JsonRuleCatalogLoader();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(catalog));
        var rules = await loader.LoadAsync(stream, CancellationToken.None);
        var snapshot = new RoutingEngine.Rules.RuleCatalogSnapshot(1, DateTimeOffset.UtcNow, rules);
        var ruleStore = new InMemoryRuleStoreStub(snapshot);

        // Capabilities do NOT include BEN for EUR
        var caps = new Dictionary<string, IReadOnlyDictionary<string, CurrencyCapability>>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEUTDEFFXXX"] = new Dictionary<string, CurrencyCapability>(StringComparer.OrdinalIgnoreCase)
            {
                ["EUR"] = new CurrencyCapability("DE12123456789012345678", new HashSet<string>(StringComparer.OrdinalIgnoreCase){"SHA","OWN"})
            }
        };
        var capSnapshot = new CorridorCapabilitiesSnapshot(1, DateTimeOffset.UtcNow, caps);
        var capStore = new CapabilitiesStoreStub(capSnapshot);

        var host = new RoutingEngineHost(ruleStore, capStore, new[] { new CapabilityPostProcessor() });
        var context = RoutingEngineTestHarness.GetContext(request);
        var result = await host.EvaluateAsync(context);

        Assert.Empty(result.GreenRoutes);
        Assert.Contains(result.RedRoutes, r => r.RuleCode == "CAPABILITY:CHARGE_BEARER_UNSUPPORTED");
    }

    [Fact]
    public async Task Post_processor_demotes_when_currency_unsupported()
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
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR", chargeBearer: "SHA");

        var loader = new JsonRuleCatalogLoader();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(catalog));
        var rules = await loader.LoadAsync(stream, CancellationToken.None);
        var snapshot = new RoutingEngine.Rules.RuleCatalogSnapshot(1, DateTimeOffset.UtcNow, rules);
        var ruleStore = new InMemoryRuleStoreStub(snapshot);

        // Capabilities missing EUR entry for the BIC
        var caps = new Dictionary<string, IReadOnlyDictionary<string, CurrencyCapability>>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEUTDEFFXXX"] = new Dictionary<string, CurrencyCapability>(StringComparer.OrdinalIgnoreCase)
            {
                ["USD"] = new CurrencyCapability("DE34987654321098765432", new HashSet<string>(StringComparer.OrdinalIgnoreCase){"SHA","OWN"})
            }
        };
        var capSnapshot = new CorridorCapabilitiesSnapshot(1, DateTimeOffset.UtcNow, caps);
        var capStore = new CapabilitiesStoreStub(capSnapshot);

        var host = new RoutingEngineHost(ruleStore, capStore, new[] { new CapabilityPostProcessor() });
        var context = RoutingEngineTestHarness.GetContext(request);
        var result = await host.EvaluateAsync(context);

        Assert.Empty(result.GreenRoutes);
        Assert.Contains(result.RedRoutes, r => r.RuleCode == "CAPABILITY:CURRENCY_UNSUPPORTED");
    }

    private sealed class InMemoryRuleStoreStub : RoutingEngine.Rules.IRuleStore
    {
        private readonly RoutingEngine.Rules.RuleCatalogSnapshot _snapshot;
        public InMemoryRuleStoreStub(RoutingEngine.Rules.RuleCatalogSnapshot snapshot) => _snapshot = snapshot;
        public Task<RoutingEngine.Rules.RuleCatalogSnapshot> GetSnapshotAsync(CancellationToken ct = default) => Task.FromResult(_snapshot);
    }

    private sealed class CapabilitiesStoreStub : ICapabilitiesStore
    {
        private readonly CorridorCapabilitiesSnapshot _snapshot;
        public CapabilitiesStoreStub(CorridorCapabilitiesSnapshot snapshot) => _snapshot = snapshot;
        public Task<CorridorCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken ct = default) => Task.FromResult(_snapshot);
    }
}
