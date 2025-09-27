using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using RoutingEngine.Exceptions;
using Serilog.Events;

namespace RoutingEngine.Configuration;

/// <summary>
/// Provides helpers to bind and validate <see cref="RoutingEngineOptions"/> from configuration sources.
/// </summary>
public static class RoutingEngineOptionsBinder
{
    private const string DefaultSectionName = "RoutingEngine";
    private const string RulesPathEnvironmentVariable = "ROUTING_RULES_PATH";
    private const string LogLevelEnvironmentVariable = "ROUTING_LOG_LEVEL";

    /// <summary>
    /// Binds the routing engine options from configuration and validates required values.
    /// </summary>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">Optional configuration section. Defaults to <c>RoutingEngine</c>.</param>
    /// <param name="baseDirectory">Optional base directory for resolving relative file paths. Defaults to <see cref="AppContext.BaseDirectory"/>.</param>
    /// <returns>An instance of <see cref="RoutingEngineOptions"/>.</returns>
    /// <exception cref="RoutingValidationException">Thrown when required configuration values are missing or invalid.</exception>
    public static RoutingEngineOptions Bind(
        IConfiguration configuration,
        string sectionName = DefaultSectionName,
        string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        var rulesFilePath = ResolveRulesFilePath(section, configuration, baseDirectory);
        var evaluationCacheSize = Math.Max(0, section.GetValue<int?>(nameof(RoutingEngineOptions.EvaluationCacheSize)) ?? 0);
        var minimumLogLevel = ResolveLogLevel(section);

        return new RoutingEngineOptions
        {
            RulesFilePath = rulesFilePath,
            EvaluationCacheSize = evaluationCacheSize,
            MinimumLogLevel = minimumLogLevel
        };
    }

    private static string ResolveRulesFilePath(IConfiguration section, IConfiguration configuration, string? baseDirectory)
    {
        var rulesPath = Environment.GetEnvironmentVariable(RulesPathEnvironmentVariable)
            ?? section.GetValue<string?>(nameof(RoutingEngineOptions.RulesFilePath))
            ?? configuration.GetValue<string?>(nameof(RoutingEngineOptions.RulesFilePath));

        if (string.IsNullOrWhiteSpace(rulesPath))
        {
            throw new RoutingValidationException("Routing rule catalog path must be provided via configuration or environment variable.");
        }

        var workingBaseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;

        var resolvedPath = Path.IsPathRooted(rulesPath)
            ? rulesPath
            : Path.Combine(workingBaseDirectory!, rulesPath);

        resolvedPath = Path.GetFullPath(resolvedPath);

        return RoutingGuards.EnsureFileExists(resolvedPath);
    }

    private static LogEventLevel ResolveLogLevel(IConfiguration section)
    {
        var logLevelValue = Environment.GetEnvironmentVariable(LogLevelEnvironmentVariable)
            ?? section.GetValue<string?>(nameof(RoutingEngineOptions.MinimumLogLevel));

        if (string.IsNullOrWhiteSpace(logLevelValue))
        {
            return LogEventLevel.Warning;
        }

        if (Enum.TryParse<LogEventLevel>(logLevelValue, true, out var parsedLevel))
        {
            return parsedLevel;
        }

        throw new RoutingValidationException($"Invalid routing log level '{logLevelValue}'.");
    }
}
