using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Recommendations;
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

    public static RecommendedGameDto ToRecommendedDto(Game game, double? score)
    {
        var result = Fill(new RecommendedGameDto(), game, true, false, false);
        result.Score = score;
        return result;
    }

    public static GameInfoResponseDto ToDto(Game game, bool withTags = true, bool withScreenshots = false, bool withSteamAppId = false)
    {
        return Fill(new GameInfoResponseDto(), game, withTags, withScreenshots, withSteamAppId);
    }

    private static T Fill<T>(T result, Game game, bool withTags, bool withScreenshots, bool withSteamAppId)
        where T : GameInfoResponseDto
    {
        result.GameId = game.Id;
        result.Title = game.Title;
        result.Description = game.Description;
        result.ReleaseDate = game.ReleaseDate;
        result.Developer = game.Developer;
        result.Publisher = game.Publisher;
        result.HeaderImageUrl = game.HeaderImageUrl;

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