namespace GReSym.Core.Exceptions;

/// <summary>The media store (Openinary) is unreachable, not configured or rejected a request. An infrastructure failure.</summary>
public class MediaStoreException : Exception
{
    public MediaStoreException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
