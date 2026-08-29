namespace GReSym.Core.Exceptions;

public class UsernameShortException : DomainException
{
    public UsernameShortException() 
        : base($"Provided Username is too short") { }
    
}