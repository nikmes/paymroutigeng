using System.Collections.Generic;

namespace RoutingEngine.Capabilities;

/// <summary>
/// Immutable snapshot of correspondent corridor capabilities with a version and timestamp.
/// </summary>
public sealed record CorridorCapabilitiesSnapshot(
    long Version,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, CurrencyCapability>> BicToCurrencyCapabilities
);

public sealed record CurrencyCapability(
    string NostroIban,
    IReadOnlySet<string> SupportedCharges // e.g., BEN, SHA, OUR
);
