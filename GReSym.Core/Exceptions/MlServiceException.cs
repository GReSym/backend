namespace GReSym.Core.Exceptions;

/// <summary>The ML service HTTP API failed. Not a <see cref="DomainException"/>: it's an infrastructure failure.</summary>
public class MlServiceException : Exception
{
    public MlServiceException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
