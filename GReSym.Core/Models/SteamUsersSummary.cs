namespace GReSym.Core.Models;

/// <summary>Size of the collected Steam user sample (games = rows in steam_user_games, i.e. in the catalogue).</summary>
public record SteamUsersSummary(int Users, int Games, int ReviewedGames, double AverageGamesPerUser);
