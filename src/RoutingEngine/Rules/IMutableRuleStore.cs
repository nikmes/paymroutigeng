using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoutingEngine.Domain;

namespace RoutingEngine.Rules;

public interface IMutableRuleStore : IRuleStore
{
    Task ReplaceAllAsync(IEnumerable<RoutingRule> rules, CancellationToken ct = default);
    Task AddOrUpdateAsync(IEnumerable<RoutingRule> rules, CancellationToken ct = default);
    Task RemoveAsync(IEnumerable<string> ruleCodes, CancellationToken ct = default);
}
