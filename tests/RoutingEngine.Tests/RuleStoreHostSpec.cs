using RoutingEngine.Domain;
using RoutingEngine.Evaluation;
using RoutingEngine.Rules;
using Xunit;

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
}
