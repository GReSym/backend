namespace GReSym.Core.Exceptions;

public class EmailOccupiedException : DomainException
{
    public EmailOccupiedException() : base("Email provided is occupied.") { }

    public EmailOccupiedException(string message) : base(message) { }
}
