using GReSym.Core.Entities.Feedback;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Exceptions;
using GReSym.Parser.Enums;
using GReSym.Parser.ETL;
using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GReSym.Parser.Services;

public class ParserEngine : IParserEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IParserStateManager _stateManager;
    private readonly ILogger<ParserEngine> _logger;
    private ParserState _currentState;
    private CancellationTokenSource? _cancellationTokenSource;
    private ParsingResult? _currentParsingResult;
    private int _requestCounter = 0;
    private Task<ParsingResult>? _parsingTask;
    private CancellationTokenSource? _externalTokenSource;
    private bool _isPaused = false;

    public event EventHandler<ParsingProgressEventArgs>? ProgressChanged;

    public ParserEngine(
        IServiceScopeFactory scopeFactory,
        IParserStateManager stateManager,
        ILogger<ParserEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<ParsingResult> ParseAsync(ParserContext context, CancellationToken externalToken)
    {
        _logger.LogInformation("Starting parser for source: {Source}", context.Source);

        // Сохраняем внешний токен для возможности возобновления
        _externalTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        // Сбрасываем флаг паузы
        _isPaused = false;

        try
        {
            // Загружаем или создаем состояние
            _currentState = await LoadOrCreateStateAsync(context, _externalTokenSource.Token);

            // Создаем результат парсинга
            _currentParsingResult = (ParsingResult?)ParsingResult.CreateStarted(context.Identifiers.Count)
                .WithMetadata("source", context.Source)
                .WithMetadata("mode", context.Mode.ToString())
                .WithMetadata("checkpoint", context.CheckpointName);

            // Выполняем парсинг
            var parseResult = await ExecuteParsingAsync(context, _externalTokenSource.Token);

            // Сохраняем финальное состояние
            await SaveFinalStateAsync(parseResult);

            _logger.LogInformation("Parsing completed: {Status}, Processed: {Processed}/{Total}",
                parseResult.Status, parseResult.ItemsProcessed, parseResult.TotalItemsToProcess);

            return parseResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Parsing was cancelled");
            return HandleCancellation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during parsing");
            return HandleCriticalError(ex);
        }
        finally
        {
            CleanupResources();
        }
    }

    private void CleanupResources()
    {
        // Не очищаем _cancellationTokenSource если мы на паузе
        if (!_isPaused)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _externalTokenSource?.Dispose();
            _externalTokenSource = null;
        }
    }

    private async Task<ParserState> LoadOrCreateStateAsync(ParserContext context, CancellationToken token)
    {
        var state = await _stateManager.LoadStateAsync(context.CheckpointName);

        if (state != null)
        {
            _logger.LogInformation("Resuming from checkpoint: {Checkpoint}", context.CheckpointName);

            // Если парсинг был завершен, создаем новое состояние
            if (state.Status == ParserStatus.Completed)
            {
                _logger.LogInformation("Previous parsing was completed, starting fresh");
                return CreateNewState(context);
            }

            // Если парсинг был на паузе, восстанавливаем из состояния
            if (state.Status == ParserStatus.Paused)
            {
                _logger.LogInformation("Resuming from paused state");
                state.Status = ParserStatus.Running;
                state.LastUpdatedAt = DateTime.UtcNow;

                // Восстанавливаем список идентификаторов если он пустой
                if (!state.PendingIdentifiers.Any() && state.TotalItems > 0 && context.Identifiers.Any())
                {
                    // Фильтруем уже обработанные (включая не найденные игры)
                    var allProcessed = new HashSet<string>(state.ProcessedIdentifiers);
                    state.PendingIdentifiers = context.Identifiers
                        .Where(id => !allProcessed.Contains(id))
                        .ToList();
                }

                return state;
            }

            // Фильтруем уже обработанные идентификаторы для инкрементального режима
            if (context.Mode == ParserMode.Incremental && state.ProcessedIdentifiers.Any())
            {
                // Включаем не найденные игры в фильтрацию
                var allProcessed = new HashSet<string>(state.ProcessedIdentifiers);
                var pendingIds = context.Identifiers
                    .Where(id => !allProcessed.Contains(id))
                    .ToList();

                context.Identifiers = pendingIds;
                _logger.LogInformation("Filtered {Processed} already processed items (including {NotFound} not found games), {Pending} remaining",
                    state.ProcessedIdentifiers.Count,
                    state.ProcessedIdentifiers.Count - state.Statistics.GetValueOrDefault("games_added", 0) - state.Statistics.GetValueOrDefault("games_updated", 0),
                    pendingIds.Count);
            }

            // Восстанавливаем PendingIdentifiers если они пустые
            if (!state.PendingIdentifiers.Any() && state.TotalItems > 0)
            {
                // Исключаем уже обработанные (включая не найденные)
                var allProcessed = new HashSet<string>(state.ProcessedIdentifiers);
                state.PendingIdentifiers = context.Identifiers
                    .Where(id => !allProcessed.Contains(id))
                    .ToList();
            }

            state.Status = ParserStatus.Running;
            state.LastUpdatedAt = DateTime.UtcNow;
            return state;
        }

        _logger.LogInformation("No checkpoint found, starting new parsing session");
        return CreateNewState(context);
    }

    private ParserState CreateNewState(ParserContext context)
    {
        return new ParserState
        {
            Id = Guid.NewGuid().ToString(),
            Source = context.Source,
            CurrentCheckpoint = context.CheckpointName,
            StartedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Status = ParserStatus.Running,
            TotalItems = context.Identifiers.Count,
            ProcessedItems = 0,
            FailedItems = 0,
            SkippedItems = 0,
            PendingIdentifiers = [.. context.Identifiers],
            ProcessedIdentifiers = new List<string>(),
            FailedIdentifiers = new List<string>(),
            SkippedIdentifiers = new List<string>(),
            Statistics = new Dictionary<string, int>
            {
                ["games_added"] = 0,
                ["games_updated"] = 0,
                ["reviews_added"] = 0,
                ["screenshots_added"] = 0,
                ["errors"] = 0,
                ["skipped"] = 0,
                ["not_found"] = 0
            }
        };
    }

    private async Task<ParsingResult> ExecuteParsingAsync(
    ParserContext context,
    CancellationToken token)
    {
        _logger.LogInformation("Starting parsing of {Count} items", context.Identifiers.Count);

        // Используем текущий список идентификаторов из состояния если они есть
        var identifiersToProcess = _currentState.PendingIdentifiers.Any()
            ? new List<string>(_currentState.PendingIdentifiers) // Создаем копию
            : new List<string>(context.Identifiers); // Или копию оригинального списка

        var totalBatches = (int)Math.Ceiling((double)identifiersToProcess.Count / context.BatchSize);
        var currentBatch = 0;
        var processedInThisSession = new List<string>(); // Отслеживаем обработанные в этой сессии

        // Обрабатываем батчи по индексам, чтобы избежать модификации коллекции во время итерации
        for (int i = 0; i < identifiersToProcess.Count; i += context.BatchSize)
        {
            currentBatch++;

            if (token.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested, stopping after batch {Batch}/{Total}",
                    currentBatch, totalBatches);
                break;
            }

            // Получаем батч по индексам
            var batch = identifiersToProcess
                .Skip(i)
                .Take(context.BatchSize)
                .ToList();

            _logger.LogDebug("Processing batch {Batch}/{Total} ({Count} items)",
                currentBatch, totalBatches, batch.Count());

            // Обрабатываем батч
            var batchResult = await ProcessBatchAsync(batch, context, token);

            // Обновляем текущий результат
            _currentParsingResult!.AddBatchLoadResult(batchResult);

            // Добавляем обработанные идентификаторы в список для последующего обновления состояния
            processedInThisSession.AddRange(batch);

            // Обновляем состояние для этого батча
            await UpdateStateAfterBatchAsync(batch, batchResult);

            // Сохраняем чекпоинт
            if (context.AutoSaveCheckpoint && ShouldSaveCheckpoint(currentBatch))
            {
                await SaveCheckpointAsync();
            }

            // Обновляем прогресс
            UpdateProgress(_currentParsingResult);

            // Небольшая задержка между батчами для снижения нагрузки
            if (currentBatch < totalBatches)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(context.DelayBetweenBatchesMs), token);
            }
        }

        // После обработки всех батчей, обновляем список оставшихся идентификаторов
        if (processedInThisSession.Any())
        {
            await FinalizeStateAfterProcessing(processedInThisSession);
        }

        // Финальная обработка результата
        return FinalizeParsingResult();
    }

    private async Task FinalizeStateAfterProcessing(List<string> processedIdentifiers)
    {
        // Удаляем обработанные идентификаторы из PendingIdentifiers
        foreach (var identifier in processedIdentifiers)
        {
            _currentState.PendingIdentifiers.Remove(identifier);
        }

        _currentState.LastUpdatedAt = DateTime.UtcNow;
        await SaveCheckpointAsync(); // Сохраняем финальное состояние
    }

    private async Task<BatchLoadResult> ProcessBatchAsync(
        IEnumerable<string> identifiers,
        ParserContext context,
        CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();

        if (context.Source.ToLower() == "steam")
        {
            var extractor = scope.ServiceProvider
                .GetRequiredService<IDataExtractor<Game>>();

            var loader = scope.ServiceProvider
                .GetRequiredService<IDataLoader<Game>>();

            return await ProcessBatchInternal(
                identifiers,
                extractor,
                loader,
                context,
                token);
        }
        else if (context.Source.ToLower() == "steam reviews")
        {
            var extractor = scope.ServiceProvider
                .GetRequiredService<IDataExtractor<Review>>();

            var loader = scope.ServiceProvider
                .GetRequiredService<IDataLoader<Review>>();

            return await ProcessBatchInternal(
                identifiers,
                extractor,
                loader,
                context,
                token);
        }

        throw new NotSupportedException(context.Source);
    }

    private async Task<BatchLoadResult> ProcessBatchInternal<T>(
        IEnumerable<string> identifiers,
        IDataExtractor<T> extractor,
        IDataLoader<T> loader,
        ParserContext context,
        CancellationToken token)
        where T : class
    {
        HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        try
        {
            _logger.LogInformation(
                "Extracting batch ({Count}) from {Source}",
                identifiers.Count(),
                extractor.SourceName);

            // EXTRACT MANY
            var extracted =
                await extractor.ExtractBatchAsync(
                    identifiers,
                    httpClient,
                    token,
                    context.ApiKey);

            if (!extracted.Any())
            {
                return BatchLoadResult.FromItems(
                    [LoadResult.AsSkipped("No data extracted")]);
            }

            // LOAD MANY
            var loadResult =
                await loader.LoadBatchAsync(
                    extracted,
                    token);

            return loadResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch processing failed");

            return BatchLoadResult.FromItems(
                [LoadResult.AsFailure(ex.Message)]);
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    private async Task UpdateStateAfterBatchAsync(IEnumerable<string> batch, BatchLoadResult batchResult)
    {
        var processedInBatch = new List<string>();
        var failedInBatch = new List<string>();
        var notFoundInBatch = new List<string>();

        foreach (var identifier in batch)
        {
            // Находим результат для этого идентификатора
            var result = batchResult.ItemResults.FirstOrDefault(r =>
                r.Metadata.TryGetValue("identifier", out var id) && id as string == identifier);

            if (result != null)
            {
                // Проверяем, является ли это случаем "игра не найдена"
                bool isGameNotFound = result.Metadata.TryGetValue("isGameNotFound", out var isNotFoundObj) &&
                                     isNotFoundObj is bool isNotFound && isNotFound;

                if (isGameNotFound)
                {
                    // Игра не найдена - добавляем в специальный список
                    notFoundInBatch.Add(identifier);
                    _currentState.Statistics["skipped"]++; // Увеличиваем счетчик пропущенных
                }
                else if (result.Success)
                {
                    processedInBatch.Add(identifier);

                    // Обновляем статистику
                    if (result.EntityType == "Game")
                    {
                        if (result.Operation == LoadOperationType.Insert)
                            _currentState.Statistics["games_added"]++;
                        else if (result.Operation == LoadOperationType.Update)
                            _currentState.Statistics["games_updated"]++;
                        else if (result.Operation == LoadOperationType.Skip)
                            _currentState.Statistics["not_found"]++;
                    }
                    else if (result.EntityType == "Review")
                    {
                        _currentState.Statistics["reviews_added"]++;
                    }
                    else if (result.EntityType == "Screenshot")
                    {
                        _currentState.Statistics["screenshots_added"]++;
                    }
                }
                else
                {
                    failedInBatch.Add(identifier);
                    _currentState.Statistics["errors"]++;
                }
            }
            else
            {
                // Если результат не найден, считаем это ошибкой
                failedInBatch.Add(identifier);
                _currentState.Statistics["errors"]++;
            }
        }

        // Обновляем списки идентификаторов
        _currentState.ProcessedIdentifiers.AddRange(processedInBatch);
        _currentState.FailedIdentifiers.AddRange(failedInBatch);

        // Добавляем не найденные игры в ProcessedIdentifiers (чтобы они не парсились в будущем)
        _currentState.ProcessedIdentifiers.AddRange(notFoundInBatch);

        _currentState.SkippedIdentifiers.AddRange(notFoundInBatch);

        // Обновляем счетчики
        _currentState.ProcessedItems += processedInBatch.Count + notFoundInBatch.Count;
        _currentState.FailedItems += failedInBatch.Count;
        _currentState.SkippedItems += notFoundInBatch.Count;

        _currentState.LastUpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    private bool ShouldSaveCheckpoint(int currentBatch)
    {
        // Сохраняем чекпоинт каждые 5 батчей или каждые 5 минут
        return currentBatch % 5 == 0 ||
               (DateTime.UtcNow - _currentState.LastUpdatedAt.GetValueOrDefault(_currentState.StartedAt)).TotalMinutes >= 5;
    }

    private async Task SaveCheckpointAsync()
    {
        try
        {
            await _stateManager.SaveStateAsync(_currentState, _currentState.CurrentCheckpoint ?? string.Empty);
            _logger.LogDebug("Checkpoint saved: {Checkpoint}", _currentState.CurrentCheckpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save checkpoint");
        }
    }

    private async Task SaveFinalStateAsync(ParsingResult result)
    {
        try
        {
            if (result.Status == ParserStatus.Completed)
            {
                _currentState.Status = ParserStatus.Completed;
                _currentState.LastUpdatedAt = DateTime.UtcNow;

                // Очищаем состояние, если парсинг завершен
                await _stateManager.ClearStateAsync(_currentState.CurrentCheckpoint ?? string.Empty);
            }
            else if (result.Status == ParserStatus.Paused || result.Status == ParserStatus.Stopped)
            {
                _currentState.Status = result.Status;
                _currentState.LastUpdatedAt = DateTime.UtcNow;
                await _stateManager.SaveStateAsync(_currentState, _currentState.CurrentCheckpoint ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save final state");
        }
    }

    private ParsingResult FinalizeParsingResult()
    {
        if (_currentParsingResult == null)
            return ParsingResult.CreateStarted(0).MarkFailure("Parsing result was not initialized");

        // Пересчитываем статистику из LoadResults
        _currentParsingResult.RecalculateStatisticsFromLoadResults();

        // Устанавливаем StateSnapshot
        _currentParsingResult.StateSnapshot = _currentState;

        // Определяем финальный статус
        if (_cancellationTokenSource?.IsCancellationRequested == true || _externalTokenSource?.IsCancellationRequested == true)
        {
            if (_isPaused)
            {
                return _currentParsingResult.MarkPaused(_currentState);
            }
            return _currentParsingResult.MarkCancelled(_currentState);
        }

        return _currentParsingResult.MarkSuccess();
    }

    private void UpdateProgress(ParsingResult result)
    {
        ProgressChanged?.Invoke(this, new ParsingProgressEventArgs
        {
            TotalItems = Math.Max(result.TotalItemsToProcess, result.ItemsProcessed),
            ProcessedItems = result.ItemsProcessed,
            CurrentSource = _currentState.Source,
            CurrentOperation = "Parsing",
            ParsingResult = result
        });
    }

    private ParsingResult HandleCancellation()
    {
        if (_currentParsingResult == null)
            return ParsingResult.CreateStarted(0).MarkCancelled(_currentState);

        _currentParsingResult.StateSnapshot = _currentState;

        if (_isPaused)
        {
            return _currentParsingResult.MarkPaused(_currentState);
        }

        return _currentParsingResult.MarkCancelled(_currentState);
    }

    private ParsingResult HandleCriticalError(Exception ex)
    {
        var result = _currentParsingResult ?? ParsingResult.CreateStarted(0);

        result.AddError(new ParsingError(ex, "ParserEngine", "System"));
        result.StateSnapshot = _currentState;

        return result.MarkFailure($"Critical error: {ex.Message}", _currentState);
    }

    public async Task PauseAsync()
    {
        _logger.LogInformation("Pausing parser");

        // Устанавливаем флаг паузы
        _isPaused = true;

        // Отменяем текущий токен
        _cancellationTokenSource?.Cancel();
        _externalTokenSource?.Cancel();

        // Сохраняем состояние как приостановленное
        _currentState.Status = ParserStatus.Paused;
        _currentState.LastUpdatedAt = DateTime.UtcNow;

        await _stateManager.SaveStateAsync(_currentState, _currentState.CurrentCheckpoint ?? string.Empty);

        _logger.LogInformation("Parser paused");
    }

    public async Task ResumeAsync()
    {
        _logger.LogInformation("Resuming parser");

        if (string.IsNullOrEmpty(_currentState.CurrentCheckpoint))
        {
            _logger.LogWarning("No checkpoint name available, cannot resume");
            return;
        }

        var state = await _stateManager.LoadStateAsync(_currentState.CurrentCheckpoint);
        if (state == null)
        {
            _logger.LogWarning("No checkpoint found: {Checkpoint}", _currentState.CurrentCheckpoint);
            return;
        }

        if (state.Status != ParserStatus.Paused)
        {
            _logger.LogWarning("Cannot resume from status: {Status}", state.Status);
            return;
        }

        // Сбрасываем флаг паузы
        _isPaused = false;

        // Восстанавливаем состояние
        _currentState = state;
        _currentState.Status = ParserStatus.Running;
        _currentState.LastUpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Parser resumed from checkpoint: {Checkpoint}", state.CurrentCheckpoint);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping parser");

        // Сбрасываем флаг паузы
        _isPaused = false;

        // Отменяем все токены
        _cancellationTokenSource?.Cancel();
        _externalTokenSource?.Cancel();

        // Сохраняем состояние как остановленное
        _currentState.Status = ParserStatus.Stopped;
        _currentState.LastUpdatedAt = DateTime.UtcNow;

        await _stateManager.SaveStateAsync(_currentState, _currentState.CurrentCheckpoint ?? string.Empty);

        // Очищаем ресурсы
        CleanupResources();

        _logger.LogInformation("Parser stopped");
    }

    public ParserState GetCurrentState()
    {
        return _currentState;
    }

    public bool IsPaused => _isPaused;

    public IEnumerable<string> GetCheckpoints()
    {
        return _stateManager.GetAvailableCheckpoints();
    }
}