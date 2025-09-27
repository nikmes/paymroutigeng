using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using RoutingEngine.Domain;

namespace RoutingEngine.Exceptions;

/// <summary>
/// Centralised guard clauses for routing engine inputs.
/// </summary>
public static class RoutingGuards
{
    public static IReadOnlyCollection<RoutingRule> EnsureRules(IEnumerable<RoutingRule>? rules, [CallerArgumentExpression("rules")] string? parameterName = null)
    {
        if (rules is null)
        {
            throw new RoutingEvaluationException($"{parameterName ?? "rules"} cannot be null.");
        }

        return rules as IReadOnlyCollection<RoutingRule> ?? rules.ToArray();
    }

    public static RoutingContext EnsureContext(RoutingContext? context, [CallerArgumentExpression("context")] string? parameterName = null)
    {
        if (context is null)
        {
            throw new RoutingEvaluationException($"{parameterName ?? "context"} cannot be null.");
        }

        if (context.Payment is null)
        {
            throw new RoutingEvaluationException("Routing context requires a payment payload.");
        }

        if (context.Counterparty is null)
        {
            throw new RoutingEvaluationException("Routing context requires counterparty details.");
        }

        if (context.Customer is null)
        {
            throw new RoutingEvaluationException("Routing context requires customer details.");
        }

        return context;
    }

    public static string EnsureFileExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new RoutingValidationException("Rules file path must be provided.");
        }

        if (!File.Exists(path))
        {
            throw new RoutingValidationException($"Rules file '{path}' was not found.");
        }

        return path;
    }
}
