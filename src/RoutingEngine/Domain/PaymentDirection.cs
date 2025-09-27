using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Specifies the movement of funds for a payment instruction.
/// </summary>
public enum PaymentDirection
{
    [EnumMember(Value = "IN")]
    In,

    [EnumMember(Value = "OUT")]
    Out,

    [EnumMember(Value = "INT")]
    International,

    [EnumMember(Value = "OWN")]
    Own
}
