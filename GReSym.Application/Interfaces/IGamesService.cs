using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Tags;

namespace GReSym.Application.Interfaces;

public interface IGamesService
{
    Task<GamesListResponseDto> GetPopularGames(GamesListRequestDto request);
    Task<GamesListResponseDto> GetGames(GamesListRequestDto request);
    Task<GameInfoResponseDto> GetGame(int id);
    Task<GameInfoResponseDto> UpdateGame(int gameId, UpdateGameInfoRequestDto request);
    Task<TagsListResponseDto> UpdateTags(int gameId, TagsListDto request);
    Task<TagsListResponseDto> GetGameTags(int id);
    Task<TagsListResponseDto> AddTags(int gameId, TagsListDto request);
    Task<TagsListResponseDto> RemoveTags(int gameId, TagsListDto request);
}