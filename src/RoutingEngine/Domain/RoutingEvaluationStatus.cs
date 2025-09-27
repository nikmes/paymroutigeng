using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Describes the overall outcome of the evaluation request.
/// </summary>
public enum RoutingEvaluationStatus
{
    [EnumMember(Value = "EVALUATED")]
    Evaluated,

    [EnumMember(Value = "NO_MATCH")]
    NoMatch
}
