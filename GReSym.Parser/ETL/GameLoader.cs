using System.Collections.Concurrent;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Base;
using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GReSym.Parser.ETL;

public class GameLoader : IDataLoader<Game>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GameLoader> _logger;


    public GameLoader(IUnitOfWork unitOfWork, ILogger<GameLoader> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoadResult> LoadAsync(Game game, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Начало загрузки игры: {Title}", game.Title);

            var operation = await ProcessGameAsync(game, cancellationToken);

            var affectedRows = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Игра успешно загружена: {Title}, операция: {Operation}",
                game.Title, operation);

            return LoadResult.AsSuccess(operation, typeof(Game).Name, affectedRows)
                .WithEntityId(game.Id.ToString());
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Ошибка базы данных при загрузке игры: {Title}", game.Title);
            return LoadResult.AsFailure($"Database error: {dbEx.Message}", game.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке игры: {Title}", game.Title);
            return LoadResult.AsFailure($"Error: {ex.Message}", game.Id.ToString());
        }
    }

    public async Task<BatchLoadResult> LoadBatchAsync(IEnumerable<Game> games, CancellationToken cancellationToken)
    {
        var results = new List<LoadResult>();

        try
        {
            _logger.LogInformation("Начало пакетной загрузки {Count} игр", games.Count());

            await _unitOfWork.BeginTransactionAsync();

            foreach (var game in games)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Пакетная загрузка прервана");
                    results.Add(LoadResult.AsFailure("Operation cancelled", game.Id.ToString()));
                    break;
                }

                try
                {
                    var operation = await ProcessGameAsync(game, cancellationToken);
                    results.Add(LoadResult.AsSuccess(operation, typeof(Game).Name)
                        .WithEntityId(game.Id.ToString()));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке игры в пакете: {Title}", game.Title);
                    results.Add(LoadResult.AsFailure($"Error: {ex.Message}", game.Id.ToString()));
                }
            }

            var affectedRows = await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Пакетная загрузка завершена. Успешно: {SuccessCount}/{TotalCount}",
                results.Count(r => r.Success), results.Count);

            var batchResult = BatchLoadResult.FromItems(results);
            batchResult.ItemResults.ForEach(r => r.WithAffectedRows(affectedRows / results.Count));

            return batchResult;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Критическая ошибка при пакетной загрузке");

            if (!results.Any())
            {
                return BatchLoadResult.SingleFailure(
                    LoadResult.AsFailure($"Batch failed: {ex.Message}"));
            }

            return BatchLoadResult.FromItems(results, $"Batch partially failed: {ex.Message}");
        }
    }

    private async Task<LoadOperationType> ProcessGameAsync(Game game, CancellationToken cancellationToken)
    {
        // Проверяем, существует ли игра
        Game? existingGame = null;

        if (game.SteamSource != null && !string.IsNullOrEmpty(game.SteamSource.SteamAppId))
        {
            existingGame = await _unitOfWork.Games.GetBySteamAppIdAsync(game.SteamSource.SteamAppId);
        }

        if (existingGame == null)
        {
            // Проверяем по названию и разработчику
            existingGame = await _unitOfWork.Games.GetByTitleAsync(game.Title);
            if (existingGame != null && existingGame.Developer != game.Developer)
            {
                existingGame = null;
            }
        }

        if (existingGame != null)
        {
            // Обновляем существующую игру
            await UpdateExistingGame(existingGame, game, cancellationToken);
            return LoadOperationType.Update;
        }
        else
        {
            // Создаем новую игру
            await CreateNewGame(game, cancellationToken);
            return LoadOperationType.Insert;
        }
    }

    private async Task CreateNewGame(Game game, CancellationToken cancellationToken)
    {
        // Устанавливаем даты создания/обновления
        game.CreatedAt = DateTime.UtcNow;
        game.UpdatedAt = DateTime.UtcNow;

        // Обрабатываем связанные сущности
        await ProcessTags(game, cancellationToken);

        // Добавляем игру
        await _unitOfWork.Games.AddAsync(game);

        _logger.LogDebug("Создана новая игра: {Title} (ID: {Id})", game.Title, game.Id);
    }

    private async Task UpdateExistingGame(Game existingGame, Game newGame, CancellationToken cancellationToken)
    {
        // Обновляем основные поля
        existingGame.Title = newGame.Title;
        existingGame.Description = newGame.Description;
        existingGame.ReleaseDate = newGame.ReleaseDate;
        existingGame.Developer = newGame.Developer;
        existingGame.Publisher = newGame.Publisher;
        existingGame.HeaderImageUrl = newGame.HeaderImageUrl;
        existingGame.UpdatedAt = DateTime.UtcNow;

        // Обновляем SteamSource
        if (newGame.SteamSource != null)
        {
            if (existingGame.SteamSource == null)
            {
                existingGame.SteamSource = newGame.SteamSource;
                existingGame.SteamSource.GameId = existingGame.Id;
            }
            else
            {
                UpdateSteamSource(existingGame.SteamSource, newGame.SteamSource);
            }
        }

        // Обновляем MetacriticSource
        if (newGame.MetacriticSource != null)
        {
            if (existingGame.MetacriticSource == null)
            {
                existingGame.MetacriticSource = newGame.MetacriticSource;
                existingGame.MetacriticSource.GameId = existingGame.Id;
            }
            else
            {
                UpdateMetacriticSource(existingGame.MetacriticSource, newGame.MetacriticSource);
            }
        }

        // Обновляем теги
        await UpdateTags(existingGame, newGame, cancellationToken);

        // Обновляем скриншоты
        await UpdateScreenshots(existingGame, newGame, cancellationToken);

        // Обновляем отзывы
        await UpdateReviews(existingGame, newGame, cancellationToken);

        _unitOfWork.Games.Update(existingGame);
        _logger.LogDebug("Обновлена существующая игра: {Title} (ID: {Id})", existingGame.Title, existingGame.Id);
    }

    private void UpdateSteamSource(SteamSource existing, SteamSource newSource)
    {
        existing.SteamAppId = newSource.SteamAppId;
        existing.Price = newSource.Price;
        existing.RecommendationsCount = newSource.RecommendationsCount;
        existing.ReleaseDate = newSource.ReleaseDate;
        existing.IsFree = newSource.IsFree;
    }

    private void UpdateMetacriticSource(MetacriticSource existing, MetacriticSource newSource)
    {
        existing.MetacriticUrl = newSource.MetacriticUrl;
        existing.Metascore = newSource.Metascore;
        existing.UserScore = newSource.UserScore;
        existing.RatingCount = newSource.RatingCount;
        existing.CriticReviewsCount = newSource.CriticReviewsCount;
        existing.UserReviewsCount = newSource.UserReviewsCount;
    }
    private async Task ProcessTags(Game game, CancellationToken cancellationToken)
    {
        if (game.GameTags == null || !game.GameTags.Any())
            return;

        var processedTags = new HashSet<string>();
        var updatedGameTags = new List<GameTag>();

        foreach (var gameTag in game.GameTags)
        {
            var tagName = gameTag.Tag.Name?.Trim();
            if (string.IsNullOrEmpty(tagName))
                continue;

            // Проверяем уникальность в рамках текущей обработки
            if (processedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                continue;

            processedTags.Add(tagName);

            var tag = await _unitOfWork.Tags.GetOrCreateAsync(tagName, cancellationToken);
            gameTag.Tag = tag;
            gameTag.TagId = tag.Id;

            updatedGameTags.Add(gameTag);
        }

        game.GameTags = updatedGameTags;
    }

    private async Task UpdateTags(Game existingGame, Game newGame, CancellationToken cancellationToken)
    {
        // Сначала обработаем теги новой игры
        await ProcessTags(newGame, cancellationToken);

        if (newGame.GameTags == null || !newGame.GameTags.Any())
        {
            existingGame.GameTags.Clear();
            _unitOfWork.Games.Update(existingGame);
            return;
        }

        // Используем стратегию: удаляем все старые теги и добавляем новые
        // Это проще и надежнее

        // Удаляем все существующие связи
        var currentTags = await _unitOfWork.GameTags
            .GetTagsByGameIdAsync(existingGame.Id);

        foreach (var currentTag in currentTags)
        {
            await _unitOfWork.GameTags.RemoveByGameAndTagAsync(
                existingGame.Id, currentTag.Id);
        }

        // Очищаем локальную коллекцию
        existingGame.GameTags.Clear();

        // Добавляем обработанные теги из новой игры
        foreach (var newGameTag in newGame.GameTags)
        {
            // Проверяем, что тег существует (после ProcessTags он должен существовать)
            if (newGameTag.Tag?.Id > 0)
            {
                existingGame.GameTags.Add(new GameTag
                {
                    GameId = existingGame.Id,
                    TagId = newGameTag.Tag.Id,
                    Game = existingGame,
                    Tag = newGameTag.Tag
                });
            }
        }

        _unitOfWork.Games.Update(existingGame);
    }

    private async Task UpdateScreenshots(Game existingGame, Game newGame, CancellationToken cancellationToken)
    {
        if (newGame.Screenshots == null || !newGame.Screenshots.Any())
            return;

        // Для простоты заменяем все скриншоты
        existingGame.Screenshots.Clear();

        foreach (var screenshot in newGame.Screenshots)
        {
            if (newGame.Screenshots.Contains(screenshot))
            {
                continue;
            }
            screenshot.GameId = existingGame.Id;
            screenshot.Game = existingGame;
            existingGame.Screenshots.Add(screenshot);
        }
    }

    private async Task UpdateReviews(Game existingGame, Game newGame, CancellationToken cancellationToken)
    {
        if (newGame.Reviews == null || !newGame.Reviews.Any())
            return;

        // Добавляем только новые отзывы (по уникальному ключу - Source + Author + CreatedAt)
        foreach (var newReview in newGame.Reviews)
        {
            newReview.GameId = existingGame.Id;
            newReview.Game = existingGame;

            // Используем репозиторий отзывов для добавления
            await _unitOfWork.Reviews.AddAsync(newReview);
        }
    }
}