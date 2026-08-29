namespace GReSym.Core.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() : base("Invalid credentials.") { }

    public InvalidCredentialsException(string message) : base(message) { }
}
