using Serilog.Events;

namespace RoutingEngine.Configuration;

/// <summary>
/// Represents configuration values required by the routing engine.
/// </summary>
public sealed record RoutingEngineOptions
{
    /// <summary>Absolute path to the routing rule catalog JSON file.</summary>
    public required string RulesFilePath { get; init; }

    /// <summary>Maximum number of compiled evaluations cached in memory (0 disables caching).</summary>
    public int EvaluationCacheSize { get; init; }

    /// <summary>Minimum Serilog log level emitted by the routing engine.</summary>
    public LogEventLevel MinimumLogLevel { get; init; } = LogEventLevel.Warning;
}
