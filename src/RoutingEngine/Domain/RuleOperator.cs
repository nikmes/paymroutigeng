using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Logical combinator applied to predicates within a routing rule.
/// </summary>
public enum RuleOperator
{
    [EnumMember(Value = "ALL")]
    All,

    [EnumMember(Value = "ANY")]
    Any,

    [EnumMember(Value = "NONE")]
    None,

    [EnumMember(Value = "ONE")]
    One
}
