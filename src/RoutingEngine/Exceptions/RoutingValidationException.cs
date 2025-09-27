namespace RoutingEngine.Exceptions;

/// <summary>
/// Represents domain validation failures encountered while parsing routing rules or requests.
/// </summary>
public sealed class RoutingValidationException : RoutingException
{
    public RoutingValidationException(string message)
        : base(message)
    {
        Errors = Array.Empty<string>();
    }

    public RoutingValidationException(string message, IEnumerable<string> errors)
        : base(message)
    {
        Errors = errors.ToArray();
    }

    public RoutingValidationException(string message, IEnumerable<string> errors, Exception innerException)
        : base(message, innerException)
    {
        Errors = errors.ToArray();
    }

    /// <summary>
    /// Detailed validation errors captured during processing.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}
