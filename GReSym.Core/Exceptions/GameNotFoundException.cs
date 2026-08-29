namespace GReSym.Core.Exceptions;

public class GameNotFoundException : DomainException
{
    public GameNotFoundException(string gameId) 
        : base($"Game with ID {gameId} was not found.") { }
}