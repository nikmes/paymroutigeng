using RoutingEngine.Domain;
using RoutingEngine.Capabilities;

namespace RoutingEngine.Evaluation;

/// <summary>
/// Post-processor that can validate and annotate a routing evaluation result.
/// </summary>
public interface IRoutePostProcessor
{
    RoutingEvaluationResult Process(RoutingContext request, RoutingEvaluationResult decision, CorridorCapabilitiesSnapshot capabilities);
}
