namespace GReSym.Application.DTO.Evaluation;

public class ImportSteamUsersResponseDto
{
    public int Imported { get; set; }

    /// <summary>Users already in the database (same SteamID hash).</summary>
    public int SkippedExisting { get; set; }

    /// <summary>Games of the imported users that are in the catalogue (stored).</summary>
    public int GamesMatched { get; set; }

    /// <summary>Games of the imported users missing from the catalogue (dropped).</summary>
    public int GamesUnmatched { get; set; }
}
