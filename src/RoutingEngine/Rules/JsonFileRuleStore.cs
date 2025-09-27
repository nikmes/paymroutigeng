using RoutingEngine.Configuration;

namespace RoutingEngine.Rules;

/// <summary>
/// File-backed rule store loading JSON catalogs via JsonRuleCatalogLoader.
/// Version increments when file write timestamp changes.
/// </summary>
public sealed class JsonFileRuleStore : IRuleStore
{
    private readonly string _path;
    private readonly JsonRuleCatalogLoader _loader;
    private DateTime _lastWriteUtc;
    private long _version;

    public JsonFileRuleStore(string path, JsonRuleCatalogLoader? loader = null)
    {
        _path = Path.GetFullPath(path);
        _loader = loader ?? new JsonRuleCatalogLoader();
        if (!File.Exists(_path))
            throw new FileNotFoundException("Rules file not found", _path);
    }

    public Task<RuleCatalogSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        var info = new FileInfo(_path);
        var write = info.LastWriteTimeUtc;
        var json = File.ReadAllText(_path);
        var rules = _loader.Load(json);

        if (write != _lastWriteUtc)
        {
            _lastWriteUtc = write;
            _version++;
            if (_version == 0) _version = 1;
        }

        var snapshot = new RuleCatalogSnapshot(
            Version: _version == 0 ? 1 : _version,
            Timestamp: DateTimeOffset.UtcNow,
            Rules: rules);
        return Task.FromResult(snapshot);
    }
}
