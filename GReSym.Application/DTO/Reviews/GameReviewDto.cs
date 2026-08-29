using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Reviews;

public class GameReviewDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Range(0, 10)]
    public int Rating { get; set; }
    [MaxLength(16384)] // Approx. 20-30 kb of data
    public string? comment { get; set; }
    public int HoursInGame { get; set; }
    public bool Recommend { get; set; }
    [Required]
    public string Source { get; set; } = string.Empty;
}