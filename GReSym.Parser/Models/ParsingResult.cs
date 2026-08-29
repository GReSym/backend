using System.Text;
using GReSym.Parser.Enums;
using Newtonsoft.Json;

namespace GReSym.Parser.Models;

[JsonObject(MemberSerialization.OptIn)]
public class ParsingResult : OperationResult
{
    // Базовый статус
    [JsonProperty("status")]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public ParserStatus Status { get; private set; }
    
    [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
    public string? Message { get; private set; }

    // Временные метки
    [JsonProperty("startedUtc")]
    public DateTime StartedUtc { get; private set; }
    
    [JsonProperty("finishedUtc")]
    public DateTime FinishedUtc { get; private set; }
    
    [JsonIgnore]
    public TimeSpan Elapsed => FinishedUtc - StartedUtc;

    // Прогресс
    [JsonProperty("totalItemsToProcess")]
    public int TotalItemsToProcess { get; private set; }
    
    [JsonProperty("itemsProcessed")]
    public int ItemsProcessed { get; private set; }
    
    [JsonProperty("itemsSuccessfullyProcessed")]
    public int ItemsSuccessfullyProcessed { get; private set; }
    
    [JsonProperty("itemsFailed")]
    public int ItemsFailed { get; private set; }

    // Результаты для БД
    [JsonProperty("gamesAdded")]
    public int GamesAdded { get; private set; }
    
    [JsonProperty("gamesUpdated")]
    public int GamesUpdated { get; private set; }
    
    [JsonProperty("reviewsAdded")]
    public int ReviewsAdded { get; private set; }
    
    [JsonProperty("screenshotsAdded")]
    public int ScreenshotsAdded { get; private set; }

    // Результаты загрузки (новое поле для иерархии)
    [JsonProperty("loadResults", NullValueHandling = NullValueHandling.Ignore)]
    public List<BatchLoadResult> LoadResults { get; private set; } = new();

    // Ошибки
    [JsonProperty("errors")]
    public IReadOnlyCollection<ParsingError> Errors => _errors.AsReadOnly();
    private readonly List<ParsingError> _errors = new();

    // Предупреждения
    [JsonProperty("warnings")]
    public IReadOnlyCollection<string> Warnings => _warnings.AsReadOnly();
    private readonly List<string> _warnings = new();

    // Состояние для возобновления
    [JsonProperty("stateSnapshot", NullValueHandling = NullValueHandling.Ignore)]
    public ParserState? StateSnapshot { get; set; }
    
    // Свойства только для чтения (не сериализуются в JSON)
    [JsonIgnore]
    public double ProgressPercentage => 
        TotalItemsToProcess > 0 ? ItemsProcessed * 100.0 / TotalItemsToProcess : 0;
    
    [JsonIgnore]
    public int SuccessRate => 
        ItemsProcessed > 0 ? ItemsSuccessfullyProcessed * 100 / ItemsProcessed : 0;
    
    [JsonIgnore]
    public bool HasErrors => _errors.Any();
    
    [JsonIgnore]
    public bool HasWarnings => _warnings.Any();
    
    // Конструктор
    private ParsingResult(bool success, string? error = null) 
        : base(success, error) { }

    // Фабричные методы
    public static ParsingResult CreateStarted(int totalItems)
    {
        return new ParsingResult(false)
        {
            Status = ParserStatus.Running,
            StartedUtc = DateTime.UtcNow,
            TotalItemsToProcess = totalItems,
            Message = "Парсинг начат"
        };
    }

    public ParsingResult MarkSuccess()
    {
        Success = true;
        Status = ParserStatus.Completed;
        FinishedUtc = DateTime.UtcNow;
        Message = "Парсинг успешно завершен";
        return this;
    }

    public ParsingResult MarkFailure(string errorMessage, ParserState? state = null)
    {
        Success = false;
        Status = ParserStatus.Failed;
        FinishedUtc = DateTime.UtcNow;
        Message = errorMessage;
        StateSnapshot = state;
        return this;
    }

    public ParsingResult MarkCancelled(ParserState state)
    {
        Success = false;
        Status = ParserStatus.Stopped;
        FinishedUtc = DateTime.UtcNow;
        Message = "Парсинг отменен";
        StateSnapshot = state;
        return this;
    }

    public ParsingResult MarkPaused(ParserState state)
    {
        Success = false;
        Status = ParserStatus.Paused;
        FinishedUtc = DateTime.UtcNow;
        Message = "Парсинг приостановлен";
        StateSnapshot = state;
        return this;
    }

    // Методы для обновления статистики
    public void IncrementProcessed() => ItemsProcessed++;
    public void IncrementSuccess() => ItemsSuccessfullyProcessed++;
    public void IncrementFailed() => ItemsFailed++;

    public void AddGameAdded() => GamesAdded++;
    public void AddGameUpdated() => GamesUpdated++;
    public void AddReview() => ReviewsAdded++;
    public void AddScreenshot() => ScreenshotsAdded++;
    
    // Новые методы для работы с LoadResult
    public void AddLoadResult(LoadResult result)
    {
        // Создаем BatchLoadResult с одним элементом или добавляем к существующему
        if (!LoadResults.Any())
        {
            LoadResults.Add(BatchLoadResult.SingleSuccess(result));
        }
        else
        {
            LoadResults.Last().AddItemResult(result);
        }
        
        // Обновляем статистику
        ItemsProcessed++;
        if (result.Success)
        {
            ItemsSuccessfullyProcessed++;
            
            // Обновляем счетчики по типам операций
            if (result.EntityType == "Game")
            {
                if (result.Operation == LoadOperationType.Insert)
                    GamesAdded++;
                else if (result.Operation == LoadOperationType.Update)
                    GamesUpdated++;
            }
            else if (result.EntityType == "Review")
            {
                ReviewsAdded++;
            }
            else if (result.EntityType == "Screenshot")
            {
                ScreenshotsAdded++;
            }
        }
        else
        {
            ItemsFailed++;
        }
    }
    
    public void AddBatchLoadResult(BatchLoadResult batchResult)
    {
        LoadResults.Add(batchResult);
        
        // Обновляем статистику
        ItemsProcessed += batchResult.TotalCount;
        ItemsSuccessfullyProcessed += batchResult.SuccessCount;
        ItemsFailed += batchResult.FailureCount;
        
        // Обновляем счетчики по типам операций
        foreach (var result in batchResult.SuccessfulItems)
        {
            if (result.EntityType == "Game")
            {
                if (result.Operation == LoadOperationType.Insert)
                    GamesAdded++;
                else if (result.Operation == LoadOperationType.Update)
                    GamesUpdated++;
            }
            else if (result.EntityType == "Review")
            {
                ReviewsAdded++;
            }
            else if (result.EntityType == "Screenshot")
            {
                ScreenshotsAdded++;
            }
        }
    }

    public void AddError(ParsingError error) => _errors.Add(error);
    public void AddError(string identifier, string source, string message)
    {
        _errors.Add(new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = message,
            OccurredAt = DateTime.UtcNow
        });
    }

    public void AddWarning(string warning) => _warnings.Add(warning);
    
    // Метод для агрегации статистики из LoadResults
    public void RecalculateStatisticsFromLoadResults()
    {
        ItemsProcessed = 0;
        ItemsSuccessfullyProcessed = 0;
        ItemsFailed = 0;
        GamesAdded = 0;
        GamesUpdated = 0;
        ReviewsAdded = 0;
        ScreenshotsAdded = 0;
        
        foreach (var batchResult in LoadResults)
        {
            ItemsProcessed += batchResult.TotalCount;
            ItemsSuccessfullyProcessed += batchResult.SuccessCount;
            ItemsFailed += batchResult.FailureCount;
            
            foreach (var result in batchResult.SuccessfulItems)
            {
                if (result.EntityType == "Game")
                {
                    if (result.Operation == LoadOperationType.Insert)
                        GamesAdded++;
                    else if (result.Operation == LoadOperationType.Update)
                        GamesUpdated++;
                }
                else if (result.EntityType == "Review")
                {
                    ReviewsAdded++;
                }
                else if (result.EntityType == "Screenshot")
                {
                    ScreenshotsAdded++;
                }
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Статус: {Status}");
        sb.AppendLine($"Сообщение: {Message}");
        sb.AppendLine($"Время выполнения: {Elapsed:hh\\:mm\\:ss}");
        sb.AppendLine($"Прогресс: {ItemsProcessed}/{TotalItemsToProcess} ({ProgressPercentage:F1}%)");
        sb.AppendLine($"Успешно обработано: {ItemsSuccessfullyProcessed} ({SuccessRate}%)");
        sb.AppendLine($"Игр добавлено: {GamesAdded}");
        sb.AppendLine($"Игр обновлено: {GamesUpdated}");
        sb.AppendLine($"Отзывов добавлено: {ReviewsAdded}");
        sb.AppendLine($"Скриншотов добавлено: {ScreenshotsAdded}");
        
        if (LoadResults.Any())
        {
            var totalBatches = LoadResults.Count;
            var totalLoads = LoadResults.Sum(r => r.TotalCount);
            var successfulLoads = LoadResults.Sum(r => r.SuccessCount);
            sb.AppendLine($"Бачей загрузки: {totalBatches}");
            sb.AppendLine($"Всего загрузок: {totalLoads} (успешно: {successfulLoads})");
        }

        if (HasErrors)
            sb.AppendLine($"Ошибок парсинга: {_errors.Count}");

        if (HasWarnings)
            sb.AppendLine($"Предупреждений: {_warnings.Count}");

        return sb.ToString();
    }
}