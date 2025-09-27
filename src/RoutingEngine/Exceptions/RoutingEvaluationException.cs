namespace RoutingEngine.Exceptions;

/// <summary>
/// Represents failures that occur while executing a routing evaluation request.
/// </summary>
public sealed class RoutingEvaluationException : RoutingException
{
    public RoutingEvaluationException(string message)
        : base(message)
    {
    }

    public RoutingEvaluationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
