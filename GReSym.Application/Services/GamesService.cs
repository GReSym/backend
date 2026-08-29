using System.Collections.Concurrent;
using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Tags;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Application.Services;

public class GamesService : IGamesService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Cache tag name : id
    private readonly ConcurrentDictionary<string, Tag> _tagCache = new(StringComparer.OrdinalIgnoreCase);

    public GamesService(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;

        InitializeTagCache();
    }

    private void InitializeTagCache()
    {
        var tags = _unitOfWork.Tags.GetAll();
        foreach (var tag in tags)
        {
            _tagCache[tag.Name] = tag;
        }
    }

    public async Task<GamesListResponseDto> GetPopularGames(GamesListRequestDto request)
    {
        int page = request.Page;

        if (page < 1)
        {
            throw new DomainException("Page cannot be less than 1.");
        }

        int limit = request.Limit;

        List<int>? includeTagIds = null;

        if (request.Tags != null)
        {
            List<string> tagList = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            includeTagIds = new List<int>();
            foreach (var t in tagList)
            {
                if (_tagCache.TryGetValue(t, out var tag))
                {
                    includeTagIds.Add(tag.Id);
                }
                else
                {
                    return new GamesListResponseDto
                    {
                        Count = 0,
                        Games = []
                    };
                }
            }
        }

        var games = await _gameRepository.GetPopularGamesAsync(page, limit, includeTagIds);

        var result = games
            .Select(GameMapping.ToDto)
            .ToList();

        return new GamesListResponseDto
        {
            Count = result.Count,
            Games = result
        };
    }
    public async Task<GamesListResponseDto> GetGames(GamesListRequestDto request)
    {
        int page = request.Page;
        string? search = request.Search;

        if (page < 1)
        {
            throw new DomainException("Page cannot be less than 1.");
        }

        int limit = request.Limit;

        IEnumerable<Game> games;
        List<int>? includeTagIds = null;

        if (request.Tags != null)
        {
            List<string> tagList = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            includeTagIds = new List<int>();
            foreach (var t in tagList)
            {
                if (_tagCache.TryGetValue(t, out var tag))
                {
                    includeTagIds.Add(tag.Id);
                }
                else
                {
                    return new GamesListResponseDto
                    {
                        Count = 0,
                        Games = []
                    };
                }
            }
        }
        
        if (search == null)
            games = await _gameRepository.GetGamesAsync(page, limit, includeTagIds);
        else
            games = await _gameRepository.SearchAsync(search.Trim(), page, limit, includeTagIds);

        var result = games
            .Select(GameMapping.ToDto)
            .ToList();

        return new GamesListResponseDto
        {
            Count = result.Count,
            Games = result
        };
    }

    public async Task<GameInfoResponseDto> GetGame(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);

        if (game == null)
        {
            throw new GameNotFoundException(id.ToString());
        }

        return GameMapping.ToDtoWithDetails(game);
    }

    private async Task<List<Tag>> ProcessTags(List<string> tags, bool addInDb = true)
    {
        var processedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tagsWithIds = new List<Tag>();

        foreach (var t in tags)
        {
            if (string.IsNullOrEmpty(t))
                continue;

            // Проверяем уникальность в рамках текущей обработки
            if (!processedTags.Add(t))
                continue;

            // Пытаемся получить из кеша
            if (_tagCache.TryGetValue(t, out var existingTag))
            {
                tagsWithIds.Add(existingTag);
                continue;
            }

            if (addInDb)
            {
                // Иначе создаём в БД
                var tag = await _unitOfWork.Tags.GetOrCreateAsync(t);

                // Добавляем в кеш
                _tagCache[tag.Name] = tag;

                tagsWithIds.Add(tag);
            }
        }

        return tagsWithIds;
    }

    private async Task UpdateTags(Game game, List<string> newTags)
    {
        if (newTags == null || newTags.Count == 0)
        {
            game.GameTags.Clear();
            return;
        }

        // Сначала обработаем теги новой игры
        var processedTags = await ProcessTags(newTags);

        // Используем стратегию: удаляем все старые теги и добавляем новые
        // Очищаем локальную коллекцию
        game.GameTags.Clear();

        // Добавляем обработанные теги из новой игры
        foreach (var tag in processedTags)
        {
            game.GameTags.Add(new GameTag
            {
                GameId = game.Id,
                TagId = tag.Id,
                Game = game,
                Tag = tag
            });
        }
    }

    public async Task<GameInfoResponseDto> UpdateGame(int gameId, UpdateGameInfoRequestDto request)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (game == null)
        {
            throw new GameNotFoundException(gameId.ToString());
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            game.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            game.Description = request.Description.Trim();
        }

        if (request.ReleaseDate != null)
        {
            game.ReleaseDate = (DateOnly)request.ReleaseDate;
        }

        if (!string.IsNullOrWhiteSpace(request.Developer))
        {
            game.Developer = request.Developer;
        }

        if (!string.IsNullOrWhiteSpace(request.Publisher))
        {
            game.Publisher = request.Publisher;
        }

        if (request.Tags != null)
        {
            await UpdateTags(game, request.Tags);
        }

        await _unitOfWork.SaveChangesAsync();

        return GameMapping.ToDtoWithDetails(game);
    }

    public async Task<TagsListResponseDto> GetGameTags(int id)
    {
        var game = await _gameRepository.GetByIdAsync(id);

        if (game == null)
        {
            throw new GameNotFoundException(id.ToString());
        }

        var tags = game.Tags;

        return TagMapping.ToDto(tags);
    }

    public async Task<TagsListResponseDto> UpdateTags(int gameId, TagsListDto request)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (game == null)
        {
            throw new GameNotFoundException(gameId.ToString());
        }

        await UpdateTags(game, request.Tags);

        await _unitOfWork.SaveChangesAsync();

        return TagMapping.ToDto(game.Tags);
    }

    public async Task<TagsListResponseDto> AddTags(int gameId, TagsListDto request)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (game == null)
        {
            throw new GameNotFoundException(gameId.ToString());
        }

        var existingTags = await ProcessTags(request.Tags);

        var existingTagIds = game.GameTags.Select(gt => gt.TagId).ToHashSet();

        var newTagsToAdd = existingTags
            .Where(t => !existingTagIds.Contains(t.Id))
            .Select(t => new GameTag
            {
                GameId = game.Id,
                TagId = t.Id,
                Game = game,
                Tag = t
            });

        foreach (var gt in newTagsToAdd)
        {
            game.GameTags.Add(gt);
        }

        await _unitOfWork.SaveChangesAsync();

        return TagMapping.ToDto(game.Tags);
    }

    public async Task<TagsListResponseDto> RemoveTags(int gameId, TagsListDto request)
    {
        var game = await _gameRepository.GetByIdAsync(gameId);

        if (game == null)
        {
            throw new GameNotFoundException(gameId.ToString());
        }

        var existingTags = await ProcessTags(request.Tags);

        var existingTagIds = game.GameTags.Select(gt => gt.TagId).ToHashSet();

        var tagsToRemove = game.GameTags
            .Where(gt => request.Tags.Contains(gt.Tag.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var gt in tagsToRemove)
        {
            game.GameTags.Remove(gt);
        }

        await _unitOfWork.SaveChangesAsync();

        return TagMapping.ToDto(game.Tags);
    }
}