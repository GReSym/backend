using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Recommendations;

public class RecommendationsRequestDto
{
    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}
