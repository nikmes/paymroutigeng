using Serilog;
using RoutingEngine.Rules;
using RoutingEngine.Domain;

namespace RoutingEngine.Evaluation;

/// <summary>
/// Host that bridges a rule store to the core engine and caches by snapshot version.
/// </summary>
public sealed class RoutingEngineHost
{
    private readonly IRuleStore _store;
    private readonly ILogger? _logger;
    private long _cachedVersion = -1;
    private RoutingEngine? _engine;

    public RoutingEngineHost(IRuleStore store, ILogger? logger = null)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<RoutingEvaluationResult> EvaluateAsync(RoutingContext context, CancellationToken ct = default)
    {
        var snapshot = await _store.GetSnapshotAsync(ct).ConfigureAwait(false);
        if (_engine is null || snapshot.Version != _cachedVersion)
        {
            _engine = new RoutingEngine(snapshot.Rules, logger: _logger);
            _cachedVersion = snapshot.Version;
        }
        return _engine.Evaluate(context);
    }
}
