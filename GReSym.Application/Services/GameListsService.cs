using GReSym.Application.DTO.GameLists;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Enums;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Application.Services;

/// <summary>
/// Planned / rejected games: set from the game page here, or by recommendation feedback (<see cref="RecommendationsService"/>).
/// They're excluded from the recommendations and sent to the ML worker as weak likes / dislikes.
/// </summary>
public class GameListsService : IGameListsService
{
    private readonly IUserGameListRepository _lists;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GameListsService(IUserGameListRepository lists, IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _lists = lists;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GameListPageDto> List(int userId, GameListPageRequestDto request)
    {
        var status = ParseStatus(request.Status);
        var entries = await _lists.GetPageAsync(userId, status, request.Page, request.Limit);

        var games = entries
            .Select(e =>
            {
                var dto = ToDto(e);
                dto.Game = GameMapping.ToDto(e.Game);
                return dto;
            })
            .ToList();

        return new GameListPageDto { Count = games.Count, Games = games };
    }

    public async Task<GameListEntryDto> Get(int userId, int gameId)
    {
        var entry = await _lists.GetAsync(userId, gameId) ?? throw new GameListEntryNotFoundException(gameId);

        return ToDto(entry);
    }

    public async Task<GameListEntryDto> Set(int userId, int gameId, string status)
    {
        var parsed = ParseStatus(status);
        if (!await _gameRepository.ExistsAsync(gameId))
            throw new GameNotFoundException(gameId.ToString());

        var entry = await SetAsync(_lists, userId, gameId, parsed);
        await _unitOfWork.SaveChangesAsync();

        return ToDto(entry);
    }

    public async Task Remove(int userId, int gameId)
    {
        var entry = await _lists.GetAsync(userId, gameId) ?? throw new GameListEntryNotFoundException(gameId);

        _lists.Remove(entry);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Puts the game in the list (moves it from the other one), without saving. The game must exist.</summary>
    internal static async Task<UserGameListEntry> SetAsync(IUserGameListRepository lists, int userId, int gameId,
        GameListStatus status)
    {
        var now = DateTime.UtcNow;
        var entry = await lists.GetAsync(userId, gameId);
        if (entry == null)
        {
            entry = new UserGameListEntry { UserId = userId, GameId = gameId, Status = status, CreatedAt = now, UpdatedAt = now };
            await lists.AddAsync(entry);
        }
        else if (entry.Status != status)
        {
            entry.Status = status;
            entry.UpdatedAt = now;
        }

        return entry;
    }

    private static GameListStatus ParseStatus(string status) =>
        GameListStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new DomainException(
                $"Unknown list status '{status}'. Use one of: {string.Join(", ", GameListStatuses.AllNames)}.");

    private static GameListEntryDto ToDto(UserGameListEntry entry) => new()
    {
        GameId = entry.GameId,
        Status = entry.Status.ToName(),
        AddedAt = entry.UpdatedAt,
    };
}
