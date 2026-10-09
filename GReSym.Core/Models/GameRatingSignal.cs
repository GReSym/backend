namespace GReSym.Core.Models;

/// <summary>A user's opinion about a game, as input for the recommendation engine. Rating is 1–10.</summary>
public record GameRatingSignal(int GameId, int? Rating, bool? Recommend);
