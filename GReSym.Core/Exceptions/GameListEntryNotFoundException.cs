namespace GReSym.Core.Exceptions;

public class GameListEntryNotFoundException : DomainException
{
    public GameListEntryNotFoundException(int gameId)
        : base($"Game {gameId} is not in the user's lists.") { }
}
