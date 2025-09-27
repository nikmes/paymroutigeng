using System.Collections.Generic;
using System.Linq;
using RoutingEngine.Capabilities;
using RoutingEngine.Domain;

namespace RoutingEngine.Evaluation;

public sealed class CapabilityPostProcessor : IRoutePostProcessor
{
    public RoutingEvaluationResult Process(RoutingContext request, RoutingEvaluationResult decision, CorridorCapabilitiesSnapshot capabilities)
    {
        if (decision.GreenRoutes.Count == 0)
        {
            return decision; // Nothing to validate
        }

    var currency = (request.Payment?.Currency ?? string.Empty).Trim().ToUpperInvariant();
    // NOTE: PaymentContext currently lacks charge-bearer; enforcement will be handled at API layer or after domain extension.

        var newGreen = new List<RouteOutcome>(decision.GreenRoutes.Count);
        var newRed = new List<RouteOutcome>(decision.RedRoutes);

        foreach (var route in decision.GreenRoutes)
        {
            if (!capabilities.BicToCurrencyCapabilities.TryGetValue(route.CorrBankBic, out var currencyMap))
            {
                newRed.Add(new RouteOutcome("CAPABILITY:CURRENCY_UNSUPPORTED", route.CorrBankBic, $"{route.CorrBankBic} does not have capabilities for {currency}"));
                continue;
            }

            if (!currencyMap.TryGetValue(currency, out var cap))
            {
                newRed.Add(new RouteOutcome("CAPABILITY:CURRENCY_UNSUPPORTED", route.CorrBankBic, $"{route.CorrBankBic} does not support currency {currency}"));
                continue;
            }

            // Charge-bearer support: if not provided by request, assume supported; otherwise verify
            // Since domain PaymentContext has no charge-bearer, we skip enforcement here and only enrich nostroIban.

            // Enrich GREEN with nostroIban by extending Description (non-breaking). For full shape, API layer will project to output contract.
            var enrichedDescription = string.IsNullOrEmpty(route.Description)
                ? $"Nostro: {cap.NostroIban}"
                : $"{route.Description} | Nostro: {cap.NostroIban}";
            newGreen.Add(route with { Description = enrichedDescription });
        }

        var finalDecision = newGreen.Count > 0 ? RoutingDecision.CanRoute : RoutingDecision.CanNotRoute;
        var status = newGreen.Count == 0 && newRed.Count == 0
            ? RoutingEvaluationStatus.NoMatch
            : RoutingEvaluationStatus.Evaluated;

        return new RoutingEvaluationResult(status, finalDecision, newGreen, newRed, decision.AuditTrail);
    }

    private static string? NormalizeChargeBearer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "BEN" => "BEN",
            "SHA" => "SHA",
            "OWN" => "OUR",
            "OUR" => "OUR",
            _ => null
        };
    }
}
