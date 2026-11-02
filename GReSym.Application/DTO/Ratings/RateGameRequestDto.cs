using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Ratings;

/// <summary>All fields are optional: an entry with neither Rating nor Recommend is a library entry (hours/comment only).</summary>
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
