using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Represents the final routing decision derived from GREEN/RED outcomes.
/// </summary>
public enum RoutingDecision
{
    [EnumMember(Value = "CAN_ROUTE")]
    CanRoute,

    [EnumMember(Value = "CAN_NOT_ROUTE")]
    CanNotRoute
}
