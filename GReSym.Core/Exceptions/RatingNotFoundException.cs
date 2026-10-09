namespace GReSym.Core.Exceptions;

public class RatingNotFoundException : DomainException
{
    public RatingNotFoundException(int gameId)
        : base($"Rating for game with ID {gameId} was not found.") { }
}
