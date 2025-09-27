using System.Threading;
using System.Threading.Tasks;

namespace RoutingEngine.Rules;

/// <summary>
/// Abstraction for retrieving a versioned rule catalog snapshot.
/// </summary>
public interface IRuleStore
{
    Task<RuleCatalogSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}
