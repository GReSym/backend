namespace GReSym.Core.Exceptions;

/// <summary>The ML service failed. Not a <see cref="DomainException"/>: it's an infrastructure failure, not a client error.</summary>
public class RecommendationEngineException : Exception
{
    public RecommendationEngineException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
