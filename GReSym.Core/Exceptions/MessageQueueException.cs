namespace GReSym.Core.Exceptions;

/// <summary>Publishing to the message broker failed (broker down, message not confirmed).</summary>
public class MessageQueueException : Exception
{
    public MessageQueueException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
