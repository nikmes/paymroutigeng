using System.Runtime.Serialization;

namespace RoutingEngine.Domain;

/// <summary>
/// Categorises the initiating customer when available.
/// </summary>
public enum CustomerType
{
    [EnumMember(Value = "INDIVIDUAL")]
    Individual,

    [EnumMember(Value = "CORPORATE")]
    Corporate
}
