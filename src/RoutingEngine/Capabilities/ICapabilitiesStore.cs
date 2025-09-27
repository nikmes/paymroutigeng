using System.Threading;
using System.Threading.Tasks;

namespace RoutingEngine.Capabilities;

/// <summary>
/// Abstraction for retrieving a versioned snapshot of correspondent corridor capabilities.
/// </summary>
public interface ICapabilitiesStore
{
    Task<CorridorCapabilitiesSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}
