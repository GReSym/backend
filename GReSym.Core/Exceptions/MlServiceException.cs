namespace GReSym.Core.Exceptions;

/// <summary>The ML service failed outside of recommendations (see <see cref="RecommendationEngineException"/>).</summary>
public class MlServiceException : Exception
{
    public MlServiceException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
