namespace GReSym.Core.Models;

/// <summary>A recommended game with its score (higher is better, see ml.md).</summary>
public record ScoredGame(int GameId, double Score);
