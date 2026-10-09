namespace GReSym.Core.Models;

/// <summary>A recommended game with its similarity score (cosine, higher is better).</summary>
public record ScoredGame(int GameId, double Score);
