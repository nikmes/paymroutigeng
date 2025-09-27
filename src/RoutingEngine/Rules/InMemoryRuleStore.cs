using RoutingEngine.Domain;

namespace RoutingEngine.Rules;

/// <summary>
/// Thread-safe in-memory rule store that supports runtime mutations.
/// </summary>
public sealed class InMemoryRuleStore : IMutableRuleStore
{
    private readonly object _gate = new();
    private RoutingRule[] _rules = Array.Empty<RoutingRule>();
    private long _version = 1;

    public InMemoryRuleStore(IEnumerable<RoutingRule>? seed = null)
    {
        if (seed is not null) _rules = seed.ToArray();
    }

    public Task<RuleCatalogSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        RoutingRule[] snapshot;
        long version;
        lock (_gate)
        {
            snapshot = _rules.ToArray();
            version = _version;
        }

        return Task.FromResult(new RuleCatalogSnapshot(
            Version: version,
            Timestamp: DateTimeOffset.UtcNow,
            Rules: snapshot));
    }

    public Task ReplaceAllAsync(IEnumerable<RoutingRule> rules, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _rules = rules.ToArray();
            _version++;
        }
        return Task.CompletedTask;
    }

    public Task AddOrUpdateAsync(IEnumerable<RoutingRule> rules, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var dict = _rules.ToDictionary(r => r.RuleCodeName, StringComparer.OrdinalIgnoreCase);
            foreach (var r in rules)
            {
                dict[r.RuleCodeName] = r;
            }
            _rules = dict.Values.ToArray();
            _version++;
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(IEnumerable<string> ruleCodes, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var del = new HashSet<string>(ruleCodes ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            _rules = _rules.Where(r => !del.Contains(r.RuleCodeName)).ToArray();
            _version++;
        }
        return Task.CompletedTask;
    }
}
