using Serilog;
using RoutingEngine.Rules;
using RoutingEngine.Domain;
using RoutingEngine.Capabilities;

namespace RoutingEngine.Evaluation;

/// <summary>
/// Host that bridges a rule store to the core engine and caches by snapshot version.
/// </summary>
public sealed class RoutingEngineHost
{
    private readonly IRuleStore _store;
    private readonly ILogger? _logger;
    private readonly ICapabilitiesStore? _capabilities;
    private readonly IReadOnlyList<IRoutePostProcessor> _postProcessors = Array.Empty<IRoutePostProcessor>();
    private long _cachedVersion = -1;
    private RoutingEngine? _engine;

    public RoutingEngineHost(IRuleStore store, ILogger? logger = null)
    {
        _store = store;
        _logger = logger;
    }

    public RoutingEngineHost(IRuleStore store, ICapabilitiesStore capabilities, IEnumerable<IRoutePostProcessor> postProcessors, ILogger? logger = null)
    {
        _store = store;
        _capabilities = capabilities;
        _postProcessors = postProcessors?.ToArray() ?? Array.Empty<IRoutePostProcessor>();
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
        var result = _engine.Evaluate(context);

        if (_capabilities is not null && _postProcessors.Count > 0)
        {
            var caps = await _capabilities.GetSnapshotAsync(ct).ConfigureAwait(false);
            foreach (var pp in _postProcessors)
            {
                result = pp.Process(context, result, caps);
            }
        }

        return result;
    }
}
