using System.Formats.Asn1;
using System.Net.Http.Json;
using GReSym.Core.Entities.GameInfo;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Enums;
using GReSym.Core.Exceptions;
using GReSym.Core.ValueObjects;
using GReSym.Parser.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GReSym.Parser.ETL;

public class SteamExtractor : IDataExtractor<Game>
{
    private readonly ILogger<SteamExtractor> _logger;
    private const string BaseUrl = "https://store.steampowered.com/api";
    private const string Lang = "russian";

    public string SourceName => "Steam";

    public SteamExtractor(ILogger<SteamExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Game> ExtractAsync(string steamAppId, HttpClient client, CancellationToken cancellationToken, string key = "")
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            _logger.LogInformation("Начало извлечения игры из Steam: AppID={AppId}, Key={Key}",
                steamAppId, string.IsNullOrEmpty(key) ? "none" : "provided");

            // Используем ключ если он предоставлен (для Steam API может быть API ключ)
            var gameDetails = await GetGameDetailsAsync(steamAppId, client, cancellationToken, key);

            if (gameDetails == null)
            {
                throw new GameNotFoundException(steamAppId);
            }

            // Преобразуем в нашу модель Game
            var game = await ConvertToGameAsync(gameDetails, steamAppId, client, cancellationToken);

            _logger.LogInformation("Успешно извлечена игра: {Title} (AppID: {AppId})", game.Title, steamAppId);

            return game;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Ошибка HTTP при извлечении AppID {AppId}: {Message}",
                steamAppId, httpEx.Message);
            throw new InvalidOperationException($"HTTP error extracting {steamAppId}: {httpEx.Message}", httpEx);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Ошибка парсинга JSON для AppID {AppId}", steamAppId);
            throw new InvalidOperationException($"JSON parsing error for {steamAppId}", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось извлечь AppID {AppId}", steamAppId);
            throw;
        }
    }

    public async Task<IEnumerable<Game>> ExtractBatchAsync(IEnumerable<string> steamAppIds, HttpClient client, CancellationToken cancellationToken, string key = "")
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        var games = new List<Game>();
        var failedIds = new List<string>();

        _logger.LogInformation("Начало пакетного извлечения {Count} игр из Steam", steamAppIds.Count());

        foreach (var appId in steamAppIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Пакетное извлечение прервано");
                break;
            }

            try
            {
                var game = await ExtractAsync(appId, client, cancellationToken, key);
                games.Add(game);

                _logger.LogDebug("Успешно извлечена игра {Title} (AppID: {AppId})", game.Title, appId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось извлечь AppID {AppId}", appId);
                failedIds.Add(appId);
            }

            // Задержка для избежания rate limiting
            await Task.Delay(500, cancellationToken);
        }

        if (failedIds.Any())
        {
            _logger.LogWarning("Не удалось извлечь {Count} игр: {AppIds}",
                failedIds.Count, string.Join(", ", failedIds));
        }

        _logger.LogInformation("Пакетное извлечение завершено: успешно {SuccessCount}/{TotalCount}",
            games.Count, steamAppIds.Count());

        return games;
    }

    public async Task<bool> TestConnectionAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            // Пробуем получить информацию о популярной игре для теста (CS:GO)
            const string testAppId = "730";

            var response = await client.GetAsync($"{BaseUrl}/appdetails?appids={testAppId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Steam API недоступен. Status Code: {StatusCode}", response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = JObject.Parse(content);

            var success = json[testAppId]?["success"]?.Value<bool>() ?? false;

            if (success)
            {
                _logger.LogInformation("Соединение с Steam API установлено успешно");
            }
            else
            {
                _logger.LogWarning("Steam API вернул неуспешный ответ");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при тестировании соединения с Steam API");
            return false;
        }
    }

    private async Task<JObject?> GetGameDetailsAsync(string steamAppId, HttpClient client,
        CancellationToken cancellationToken, string apiKey = "")
    {
        // Формируем URL с учетом API ключа если он предоставлен
        string url;
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Если есть API ключ, можно использовать другие методы Steam API
            // Например, для получения дополнительных данных
            url = $"{BaseUrl}/appdetails?appids={steamAppId}&l={Lang}&key={apiKey}";
            _logger.LogDebug("Используется Steam API ключ");
        }
        else
        {
            // Стандартный запрос без ключа (работает для основной информации)
            url = $"{BaseUrl}/appdetails?appids={steamAppId}&l={Lang}";
        }

        _logger.LogDebug("Запрос к Steam API: {Url}", url);

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JObject.Parse(content);

        var gameData = json[steamAppId];
        if (gameData == null)
        {
            throw new GameNotFoundException(steamAppId);
        }

        var success = gameData["success"]?.Value<bool>() ?? false;
        if (!success)
        {
            // Если есть API ключ, проверяем ошибки API
            if (!string.IsNullOrEmpty(apiKey))
            {
                var errorMessage = gameData["error"]?.Value<string>();
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogWarning("Steam API вернул ошибку: {ErrorMessage}", errorMessage);
                }
            }

            throw new GameNotFoundException(steamAppId);
        }

        return gameData["data"] as JObject;
    }

    private async Task<Game> ConvertToGameAsync(JObject gameData, string steamAppId, HttpClient client, CancellationToken token)
    {
        var game = new Game
        {
            Title = gameData["name"]?.Value<string>() ?? "Unknown",
            Description = gameData["detailed_description"]?.Value<string>() ?? string.Empty,
            Developer = ExtractDeveloper(gameData),
            Publisher = ExtractPublisher(gameData),
            HeaderImageUrl = gameData["header_image"]?.Value<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var releaseDate = gameData["release_date"];
        // Release Date
        if (releaseDate != null)
        {
            var dateStr = releaseDate["date"]?.Value<string>();

            if (!string.IsNullOrEmpty(dateStr))
            {
                // Пробуем разные форматы даты
                if (DateOnly.TryParse(dateStr, out var date))
                {
                    game.ReleaseDate = date;
                }
                else
                {
                    // Пробуем извлечь год из строки
                    var yearMatch = System.Text.RegularExpressions.Regex.Match(dateStr, @"\b(19|20)\d{2}\b");
                    if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
                    {
                        game.ReleaseDate = new DateOnly(year, 1, 1);
                    }
                }
            }
        }

        // Если не удалось извлечь дату релиза, устанавливаем минимальную
        if (game.ReleaseDate == default)
        {
            game.ReleaseDate = new DateOnly(1970, 1, 1);
        }

        // Steam Source Information
        game.SteamSource = new SteamSource
        {
            SteamAppId = steamAppId,
            Price = ExtractPrice(gameData),
            RecommendationsCount = gameData["recommendations"]?["total"]?.Value<int>(),
            ReleaseDate = game.ReleaseDate,
            IsFree = gameData["is_free"]?.Value<bool>()
        };

        // Tags
        await ExtractTagsAsync(
            game,
            gameData,
            steamAppId,
            client,
            token);

        // Screenshots
        await ExtractScreenshotsAsync(game, gameData);

        return game;
    }

    // Остальные методы остаются без изменений
    private string ExtractDeveloper(JObject gameData)
    {
        try
        {
            var developers = gameData["developers"] as JArray;
            return developers != null && developers.Any()
                ? string.Join(", ", developers.Select(d => d.Value<string>()))
                : "Unknown Developer";
        }
        catch
        {
            return "Unknown Developer";
        }
    }

    private string ExtractPublisher(JObject gameData)
    {
        try
        {
            var publishers = gameData["publishers"] as JArray;
            return publishers != null && publishers.Any()
                ? string.Join(", ", publishers.Select(p => p.Value<string>()))
                : ExtractDeveloper(gameData); // Если издатель не указан, используем разработчика
        }
        catch
        {
            return ExtractDeveloper(gameData);
        }
    }

    private decimal? ExtractPrice(JObject gameData)
    {
        try
        {
            var priceOverview = gameData["price_overview"];
            if (priceOverview != null)
            {
                // Steam возвращает цену в центах/копейках
                var priceInCents = priceOverview["final"]?.Value<decimal>() ?? 0;
                return priceInCents / 100; // Конвертируем в основную валюту
            }

            // Если игра бесплатная
            if (gameData["is_free"]?.Value<bool>() == true)
            {
                return 0;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task ExtractTagsAsync(
        Game game,
        JObject gameData,
        string steamAppId,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Получение тегов
            try
            {
                var url = $"https://store.steampowered.com/app/{steamAppId}?l={Lang}";

                var html = await client.GetStringAsync(url, cancellationToken);

                var matches = System.Text.RegularExpressions.Regex.Matches(
                    html,
                    @"<a[^>]*class=""app_tag""[^>]*>\s*(.*?)\s*<\/a>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var tag = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value)
                        .Replace("\t", "")
                        .Replace("\n", "")
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(tag) && tag.Length < 40)
                        tags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Ошибка при обработке тегов в HTML {Title}",
                    game.Title);
            }

            if (tags.Count == 0)
            {
                _logger.LogWarning(
                    "Не удалось получить Steam tags через HTML для {Title}",
                    game.Title);
            }

            // Берём категории
            var categories = gameData["categories"] as JArray;

            if (categories != null)
            {
                foreach (var category in categories)
                {
                    var categoryName = category["description"]?.Value<string>();

                    if (!string.IsNullOrWhiteSpace(categoryName)
                        && categoryName.Length < 40)
                    {
                        tags.Add(categoryName.Trim());
                    }
                }
            }

            // Сохраняем теги
            foreach (var tagName in tags)
            {
                game.GameTags.Add(new GameTag
                {
                    Tag = new Tag { Name = tagName }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Ошибка при извлечении тегов для игры {Title}",
                game.Title);
        }
    }

    private async Task ExtractScreenshotsAsync(Game game, JObject gameData)
    {
        try
        {
            var screenshots = gameData["screenshots"] as JArray;
            if (screenshots == null || !screenshots.Any())
                return;

            foreach (var screenshot in screenshots.Take(10)) // Ограничиваем 10 скриншотами
            {
                var screenshotData = screenshot as JObject;
                if (screenshotData != null)
                {
                    // Пробуем получить полное изображение, иначе миниатюру
                    var url = screenshotData["path_full"]?.Value<string>() ??
                              screenshotData["path_thumbnail"]?.Value<string>();

                    if (string.IsNullOrEmpty(url))
                        continue;

                    var screenshotEntity = new Screenshot
                    {
                        Url = url,
                        Width = screenshotData["width"]?.Value<int>(),
                        Height = screenshotData["height"]?.Value<int>(),
                        Source = ScreenshotSource.Steam,
                        Caption = null // Steam не предоставляет описания для скриншотов
                    };

                    game.Screenshots.Add(screenshotEntity);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при извлечении скриншотов для игры {Title}", game.Title);
        }
    }
}