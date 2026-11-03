using System.ComponentModel.DataAnnotations;
using GReSym.Application.DTO.Games;

namespace GReSym.Application.DTO.GameLists;

/// <summary>A game in the user's planned or rejected list.</summary>
public class GameListEntryDto
{
    [Required]
    public int GameId { get; set; }

    /// <summary>"planned" or "rejected".</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>When the game was put in this list.</summary>
    [Required]
    public DateTime AddedAt { get; set; }

    // Only in lists
    public GameInfoResponseDto? Game { get; set; }
}

public class GameListPageDto
{
    /// <summary>Items on this page.</summary>
    [Required]
    public int Count { get; set; }

    [Required]
    public List<GameListEntryDto> Games { get; set; } = [];
}

public class GameListPageRequestDto
{
    /// <summary>"planned" or "rejected".</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}

public class SetGameListStatusRequestDto
{
    /// <summary>"planned" or "rejected".</summary>
    [Required]
    public string Status { get; set; } = string.Empty;
}
