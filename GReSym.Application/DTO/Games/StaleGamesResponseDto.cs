using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Games;

public class StaleGameDto
{
    [Required]
    public int GameId { get; set; }

    /// <summary>Higher = more worth re-vectorizing (sum over reasons).</summary>
    [Required]
    public double Score { get; set; }

    /// <summary>description, sentiment, reviews, growth, outdated, missing, deleted.</summary>
    [Required]
    public List<string> Reasons { get; set; } = [];

    /// <summary>When the current vector was built (UTC); null if the game has none.</summary>
    public DateTime? VectorizedAt { get; set; }

    /// <summary>Numbers behind the reasons, e.g. overlap, shareBefore/shareNow, steamReviewsBefore/Now.</summary>
    [Required]
    public Dictionary<string, double?> Details { get; set; } = [];
}

public class StaleGamesResponseDto
{
    /// <summary>Milvus collection and text version the vectors are compared against.</summary>
    [Required]
    public string Collection { get; set; } = string.Empty;

    [Required]
    public int TextVersion { get; set; }

    [Required]
    public int Vectors { get; set; }

    [Required]
    public int Games { get; set; }

    /// <summary>Stale games per reason, over all games (before the reason filter).</summary>
    [Required]
    public Dictionary<string, int> Counts { get; set; } = [];

    /// <summary>Games matching the filter; Items is one page of them.</summary>
    [Required]
    public int Total { get; set; }

    [Required]
    public List<StaleGameDto> Items { get; set; } = [];
}
