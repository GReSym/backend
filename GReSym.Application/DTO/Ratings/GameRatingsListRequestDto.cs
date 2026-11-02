using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Ratings;

public class GameRatingsListRequestDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int Limit { get; set; } = 20;
    /// <summary>Only entries with more than this many hours played.</summary>
    [Range(0, 1_000_000)]
    public int? MinHours { get; set; }
    /// <summary>true: only entries without a recommend value (not evaluated yet).</summary>
    public bool Unevaluated { get; set; }
}
