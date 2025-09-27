using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Represents the effect of a routing rule when its conditions match.
/// </summary>
public enum OutcomePolicy
{
    [EnumMember(Value = "PassOnMatch")]
    PassOnMatch,

    [EnumMember(Value = "FailOnMatch")]
    FailOnMatch
}
