using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Tags;

public class TagsListResponseDto
{
    [Required]
    public int Count { get; set; }
    [Required]
    public List<string> Tags { get; set; } = [];
}