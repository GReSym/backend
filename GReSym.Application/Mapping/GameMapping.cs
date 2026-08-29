using GReSym.Application.DTO.Games;
using GReSym.Core.Entities.GameInfo;

namespace GReSym.Application.Mapping;

public static class GameMapping
{
    public static GameInfoResponseDto ToDto(Game game)
    {
        return ToDto(game, true, false, false);
    }

    public static GameInfoResponseDto ToDtoWithDetails(Game game)
    {
        return ToDto(game, true, true, true);
    }

    public static GameInfoResponseDto ToDto(Game game, bool withTags = true, bool withScreenshots = false, bool withSteamAppId = false)
    {
        var result = new GameInfoResponseDto
        {
            GameId = game.Id,
            Title = game.Title,
            Description = game.Description,
            ReleaseDate = game.ReleaseDate,
            Developer = game.Developer,
            Publisher = game.Publisher,
            HeaderImageUrl = game.HeaderImageUrl
        };

        if (withTags && game.Tags.Count != 0)
        {
            result.Tags = [.. game.Tags.Select(t => t.Name)];
        }

        if (withScreenshots && game.Screenshots.Count != 0)
        {
            result.Screenshots = [.. game.Screenshots.Select(s => s.Url)];
        }

        if (withSteamAppId && game.SteamSource != null)
        {
            result.SteamAppId = game.SteamSource.SteamAppId;
        }

        return result;
    }
}