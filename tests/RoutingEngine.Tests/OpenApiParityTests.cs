using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RoutingEngine.Tests.Infrastructure;
using Xunit;

namespace RoutingEngine.Tests;

public sealed class OpenApiParityTests
{
    private static readonly string[] ResponsePropertyNames = LoadResponsePropertyNames();

    [Fact]
    public async Task Engine_response_contains_all_fields_defined_in_openapi_contract()
    {
        var catalog = RuleCatalogBuilder.BuildJson(
            new RuleDefinition
            {
                RuleCodeName = "RULE-OPENAPI",
                RuleDescription = "Demo rule",
                OutcomePolicy = "PassOnMatch",
                Operator = "ALL",
                PriorityWeight = 10,
                CorrBankBic = "BNPAFRPPXXX",
                PaymentCurrency = "EUR"
            });

        var request = RoutingRequestFactory.Create(direction: "OUT", currency: "EUR");

        var result = await RoutingEngineTestHarness.EvaluateAsync(catalog, request);
        var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var responseProperties = json.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyName in ResponsePropertyNames)
        {
            Assert.True(responseProperties.Contains(propertyName), $"Missing property '{propertyName}' defined in openapi.yaml");
        }
    }

    private static string[] LoadResponsePropertyNames()
    {
        var openApiPath = Path.Combine("specs", "002-payment-routing", "contracts", "openapi.yaml");
        var lines = File.ReadAllLines(openApiPath);
        var names = new List<string>();
        var inSchema = false;
        var inProperties = false;
        var propertiesIndent = -1;

        foreach (var rawLine in lines)
        {
            var trimmedStart = rawLine.TrimStart();

            if (!inSchema)
            {
                if (trimmedStart.StartsWith("RoutingEvaluationResponse:"))
                {
                    inSchema = true;
                }

                continue;
            }

            if (trimmedStart.StartsWith("RouteOutcome:"))
            {
                break;
            }

            if (!inProperties)
            {
                if (trimmedStart.StartsWith("properties:"))
                {
                    inProperties = true;
                    propertiesIndent = CountIndent(rawLine);
                }

                continue;
            }

            var indent = CountIndent(rawLine);
            if (indent <= propertiesIndent)
            {
                inProperties = false;
                continue;
            }

            var trimmed = trimmedStart;
            if (trimmed.EndsWith(":"))
            {
                var name = trimmed.TrimEnd(':').Trim();
                if (!string.IsNullOrWhiteSpace(name)
                    && !name.Contains(" ")
                    && !string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }
        }

        return names.ToArray();
    }

    private static int CountIndent(string line)
    {
        var count = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') count++;
            else break;
        }

        return count;
    }
}
