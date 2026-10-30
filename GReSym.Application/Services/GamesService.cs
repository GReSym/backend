using GReSym.Core.Models;
using System.Collections.Concurrent;
using GReSym.Application.DTO.Games;
using GReSym.Application.DTO.Tags;
using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Application.Mapping;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GReSym.Application.Services;

public class GamesService : IGamesService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameVectorizationQueue _vectorizationQueue;
    private readonly IGameVectorIndex _vectorIndex;
    private readonly IGameSearchIndex _searchIndex;
    private readonly ILogger<GamesService> _logger;

    // Search results beyond this are noise: the ML service ranks this many and the pages are cut from them
    private const int SearchResultLimit = 200;

    // Cache tag name : id
    private readonly ConcurrentDictionary<string, Tag> _tagCache = new(StringComparer.OrdinalIgnoreCase);

    public GamesService(
        IGameRepository gameRepository,
        IUnitOfWork unitOfWork,
        IGameVectorizationQueue vectorizationQueue,
        IGameVectorIndex vectorIndex,
        IGameSearchIndex searchIndex,
        ILogger<GamesService> logger)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
        _vectorizationQueue = vectorizationQueue;
        _vectorIndex = vectorIndex;
        _searchIndex = searchIndex;
        _logger = logger;

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
        
        if (string.IsNullOrWhiteSpace(search))
            games = await _gameRepository.GetGamesAsync(page, limit, includeTagIds);
        else
            games = await SearchGames(search.Trim(), request.Exact, page, limit, includeTagIds);

        var result = games
            .Select(GameMapping.ToDto)
            .ToList();

        return new GamesListResponseDto
        {
            Count = result.Count,
            Games = result
        };
    }

    /// <summary>Relevance-ranked search of the ML service (the top <see cref="SearchResultLimit"/> games, paged here),
    /// or the substring search if the ML service is unavailable.</summary>
    private async Task<IEnumerable<Game>> SearchGames(string search, bool exact, int page, int limit, List<int>? tagIds)
    {
        IReadOnlyList<int> rankedIds;
        try
        {
            rankedIds = await _searchIndex.SearchAsync(search, exact, SearchResultLimit);
        }
        catch (MlServiceException e)
        {
            _logger.LogWarning(e, "ML search failed, falling back to substring search for {Search}", search);
            return await _gameRepository.SearchAsync(search, page, limit, tagIds);
        }

        if (tagIds != null && tagIds.Count > 0)
        {
            var matching = (await _gameRepository.FilterIdsByTagsAsync(rankedIds.ToList(), tagIds)).ToHashSet();
            rankedIds = rankedIds.Where(matching.Contains).ToList();
        }

        var pageIds = rankedIds.Skip((page - 1) * limit).Take(limit).ToList();
        var games = (await _gameRepository.GetByIdsAsync(pageIds)).ToDictionary(g => g.Id);

        return pageIds.Where(games.ContainsKey).Select(id => games[id]).ToList();
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

        // Vector text = title + description (see ml/src/ml/text.py)
        var oldTitle = game.Title;
        var oldDescription = game.Description;

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

        if (game.Title != oldTitle || game.Description != oldDescription)
        {
            await TryEnqueueVectorization(game.Id, "game-updated");
        }

        return GameMapping.ToDtoWithDetails(game);
    }

    public async Task<VectorizeGamesResponseDto> EnqueueVectorization(VectorizeGamesRequestDto request)
    {
        var ids = request.GameIds.Where(id => id > 0).Distinct().ToList();

        if (ids.Count == 0)
            throw new DomainException("No valid game ids.");

        // Ids missing in MariaDB are fine: the ML worker removes their vectors
        await _vectorizationQueue.EnqueueAsync(ids, "admin-request");

        return new VectorizeGamesResponseDto { Queued = ids.Count };
    }

    public async Task<StaleGamesResponseDto> GetStaleVectors(IReadOnlyCollection<string> reasons, int limit, int offset,
        CancellationToken cancellationToken = default)
    {
        var unknown = reasons.Where(r => !StaleReasons.All.Contains(r)).ToList();
        if (unknown.Count > 0)
            throw new DomainException(
                $"Unknown reason(s): {string.Join(", ", unknown)}. Known: {string.Join(", ", StaleReasons.All)}.");
        if (limit is < 1 or > 1000)
            throw new DomainException("Limit must be between 1 and 1000.");
        if (offset < 0)
            throw new DomainException("Offset must not be negative.");

        var report = await _vectorIndex.GetStaleGamesAsync(reasons, limit, offset, cancellationToken);

        return new StaleGamesResponseDto
        {
            Collection = report.Collection,
            TextVersion = report.TextVersion,
            Vectors = report.Vectors,
            Games = report.Games,
            Counts = report.Counts.ToDictionary(),
            Total = report.Total,
            Items = report.Items.Select(i => new StaleGameDto
            {
                GameId = i.GameId,
                Score = i.Score,
                Reasons = i.Reasons.ToList(),
                VectorizedAt = i.VectorizedAt,
                Details = i.Details.ToDictionary(),
            }).ToList(),
        };
    }

    /// <summary>The game update is already saved, so a broker failure must not fail the request.</summary>
    private async Task TryEnqueueVectorization(int gameId, string reason)
    {
        try
        {
            await _vectorizationQueue.EnqueueAsync([gameId], reason);
        }
        catch (MessageQueueException e)
        {
            _logger.LogWarning(e, "Could not queue vectorization of game {GameId}; its vector is stale until re-queued", gameId);
        }
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