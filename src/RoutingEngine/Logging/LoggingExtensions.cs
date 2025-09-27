using System;
using System.Globalization;
using System.Linq;
using RoutingEngine.Domain;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace RoutingEngine.Logging;

/// <summary>
/// Provides Serilog helpers for configuring routing engine logging and emitting evaluation telemetry.
/// </summary>
public static class LoggingExtensions
{
    private const string EvaluationTemplate = "Routing evaluation completed in {Elapsed} with decision {Decision}";

    /// <summary>
    /// Configures Serilog with routing defaults (enrichment and console sink).
    /// </summary>
    public static LoggerConfiguration ConfigureRoutingDefaults(this LoggerConfiguration configuration, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Emits a structured log event describing the outcome of a routing evaluation.
    /// </summary>
    public static void LogRoutingEvaluation(this ILogger logger, RoutingEvaluationResult result, TimeSpan elapsed)
    {
        if (logger is null)
        {
            return;
        }

        var auditTrail = result.AuditTrail.Select(item => new
        {
            item.RuleCode,
            item.OutcomePolicy,
            MatchResult = item.Matched,
            item.PriorityWeight,
            item.CorrBankBic,
            item.EvaluationDuration
        }).ToArray();

        var logLevel = result.Decision == RoutingDecision.CanNotRoute || result.Status == RoutingEvaluationStatus.NoMatch
            ? LogEventLevel.Warning
            : LogEventLevel.Information;

        using (LogContext.PushProperty("RoutingAuditTrail", auditTrail, true))
        using (LogContext.PushProperty("RoutingGreenRoutes", result.GreenRoutes, true))
        using (LogContext.PushProperty("RoutingRedRoutes", result.RedRoutes, true))
        {
            logger.Write(logLevel, EvaluationTemplate, elapsed, result.Decision);
        }
    }
}
