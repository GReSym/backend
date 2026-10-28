namespace GReSym.Application.DTO.Evaluation;

public class SteamUsersSummaryDto
{
    public int Users { get; set; }

    /// <summary>Stored user games (only those in the catalogue).</summary>
    public int Games { get; set; }

    /// <summary>Stored user games with the user's own review.</summary>
    public int ReviewedGames { get; set; }

    public double AverageGamesPerUser { get; set; }
}
