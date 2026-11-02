using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Tags;

public class TagSuggestionsRequestDto
{
    /// <summary>Part of a tag name (case-insensitive); empty = all tags, shortest names first.</summary>
    [MaxLength(100)]
    public string? Search { get; set; }
    [Range(1, 100)]
    public int Limit { get; set; } = 10;
}
