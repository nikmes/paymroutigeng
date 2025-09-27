namespace RoutingEngine.Exceptions;

/// <summary>
/// Base exception type for routing engine errors.
/// </summary>
public abstract class RoutingException : Exception
{
    protected RoutingException(string message)
        : base(message)
    {
    }

    protected RoutingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
