using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Indicates whether a routing rule is considered during evaluation.
/// </summary>
public enum RuleStatus
{
    [EnumMember(Value = "ON")]
    On,

    [EnumMember(Value = "OFF")]
    Off
}
