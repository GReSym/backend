using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Ratings;

/// <summary>At least one of Rating or Recommend is required.</summary>
public class RateGameRequestDto
{
    [Range(1, 10)]
    public int? Rating { get; set; }
    public bool? Recommend { get; set; }
    [Range(0, 1_000_000)]
    public int? PlayerHours { get; set; }
    [MaxLength(2000)]
    public string? Comment { get; set; }
}
