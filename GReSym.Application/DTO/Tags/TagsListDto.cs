using System.ComponentModel.DataAnnotations;

namespace GReSym.Application.DTO.Tags;

public class TagsListDto
{
    [Required]
    public List<string> Tags { get; set; } = [];
}