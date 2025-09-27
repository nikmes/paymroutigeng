using System.Text.Json;
using RoutingEngine.Tests.Infrastructure;
using Xunit;

namespace RoutingEngine.Tests;

public sealed class LibraryContractTests
{
    [Fact]
    public async Task Evaluation_result_serializes_to_expected_contract_shape()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-PASS",
                RuleDescription = "Pass when payment matches",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 100,
                CorrBankBic = "DEUTDEFFXXX",
                PaymentDirection = "OUT"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.True(json.RootElement.TryGetProperty("decision", out _), "decision property missing");
        Assert.True(json.RootElement.TryGetProperty("greenRoutes", out _), "greenRoutes property missing");
        Assert.True(json.RootElement.TryGetProperty("redRoutes", out _), "redRoutes property missing");
        Assert.True(json.RootElement.TryGetProperty("status", out _), "status property missing");
    }
}
