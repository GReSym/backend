using GReSym.Application.DTO.Tags;
using GReSym.Core.Entities.GameInfo;

namespace GReSym.Application.Mapping;

public static class TagMapping
{
    public static TagsListResponseDto ToDto(ICollection<Tag> tags)
    {
        return new TagsListResponseDto
        {
            Count = tags.Count,
            Tags = tags.Select(t => t.Name).ToList()
        };
    }
}