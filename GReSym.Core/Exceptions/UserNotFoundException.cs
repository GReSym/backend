namespace GReSym.Core.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(int userId) 
        : base($"User with ID {userId} was not found.") { }
    
    public UserNotFoundException(string email) 
        : base($"User with email {email} was not found.") { }
}