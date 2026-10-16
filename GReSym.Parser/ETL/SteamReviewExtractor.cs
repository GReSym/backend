using System.Net.Http.Json;
using System.Xml;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Parser.Interfaces;
using Microsoft.Extensions.Logging;

namespace GReSym.Parser.ETL;

public class SteamReviewResponse
{
    public int Success { get; set; }
    public QuerySummary Query_Summary { get; set; } = default!;
    public List<SteamReviewDto> Reviews { get; set; } = [];
    public string Cursor { get; set; } = "*";
}

public class QuerySummary
{
    public int Num_Reviews { get; set; }
    // Only returned for the first page (cursor=*)
    public int? Total_Positive { get; set; }
    public int? Total_Negative { get; set; }
}

public class SteamReviewDto
{
    public string Recommendationid { get; set; } = "";
    public SteamAuthor Author { get; set; } = default!;
    public string Review { get; set; } = "";
    public bool Voted_Up { get; set; }
    public long Timestamp_Created { get; set; }
    public long Timestamp_Updated { get; set; }
    public int Votes_Up { get; set; }
    public int Votes_Funny { get; set; }
    public double Weighted_Vote_Score { get; set; }  // sent as a JSON string, Web defaults read it
    public bool Written_During_Early_Access { get; set; }
}

public class SteamAuthor
{
    public string Personaname { get; set; } = "";
    public int? Playtime_At_Review { get; set; }  // minutes
}

public class SteamReviewExtractor : IDataExtractor<Review>
{
    private readonly ILogger<SteamReviewExtractor> _logger;
    private readonly IUnitOfWork _uow;

    private const string BaseUrl =
        "https://store.steampowered.com/appreviews";

    private const string Lang = "russian";
    private const int ReviewsPerType = 5;

    public string SourceName => "Steam Reviews";

    public SteamReviewExtractor(
        ILogger<SteamReviewExtractor> logger,
        IUnitOfWork uow)
    {
        _logger = logger;
        _uow = uow;
    }

    // =========================
    // BATCH
    // =========================
    public async Task<IEnumerable<Review>> ExtractBatchAsync(
        IEnumerable<string> identifiers,
        HttpClient client,
        CancellationToken token,
        string key = "")
    {
        var reviews = new List<Review>();

        foreach (var id in identifiers)
        {
            if (token.IsCancellationRequested)
                break;

            try
            {
                var gameReviews =
                    await ExtractReviewsForGame(id, client, token);

                reviews.AddRange(gameReviews);

                await Task.Delay(300, token); // anti-rate-limit
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed extracting reviews for GameId {GameId}", id);
            }
        }

        return reviews;
    }

    // =========================
    // CORE LOGIC
    // =========================
    private async Task<List<Review>> ExtractReviewsForGame(
        string gameIdStr,
        HttpClient client,
        CancellationToken token)
    {
        if (!int.TryParse(gameIdStr, out var gameId))
            return [];

        var game = await _uow.Games.GetByIdAsync(gameId);

        if (game?.SteamSource?.SteamAppId == null)
        {
            _logger.LogDebug(
                "Game {GameId} has no SteamSource", gameId);

            return [];
        }

        var appId = game.SteamSource.SteamAppId;

        _logger.LogInformation(
            "Extracting reviews for GameId={GameId}, AppId={AppId}",
            gameId, appId);

        await UpdateReviewTotals(game.SteamSource, client, token);

        var reviews = new List<Review>();

        reviews.AddRange(
            await FetchReviews(appId, gameId, true, client, token));

        reviews.AddRange(
            await FetchReviews(appId, gameId, false, client, token));

        return reviews;
    }

    // =========================
    // TOTALS (ALL LANGUAGES)
    // =========================
    // We only store a small sample of positive and negative reviews, so the real ratio comes from Steam's summary.
    // The ML service uses it to mix the reviews it embeds and to notice sentiment shifts.
    private async Task UpdateReviewTotals(
        SteamSource steamSource,
        HttpClient client,
        CancellationToken token)
    {
        try
        {
            var url =
                $"{BaseUrl}/{steamSource.SteamAppId}" +
                $"?json=1" +
                $"&language=all" +
                $"&purchase_type=all" +
                $"&num_per_page=1";

            var response =
                await client.GetFromJsonAsync<SteamReviewResponse>(url, token);

            if (response?.Success != 1 || response.Query_Summary?.Total_Positive == null)
                return;

            // The game was loaded tracked by this batch's UnitOfWork, so this is a plain UPDATE of steam_source
            steamSource.ReviewsPositive = response.Query_Summary.Total_Positive;
            steamSource.ReviewsNegative = response.Query_Summary.Total_Negative;
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Failed fetching review totals for AppId {AppId}", steamSource.SteamAppId);
        }
    }

    // =========================
    // FETCH POSITIVE / NEGATIVE
    // =========================
    private async Task<IEnumerable<Review>> FetchReviews(
        string appId,
        int gameId,
        bool positive,
        HttpClient client,
        CancellationToken token)
    {
        var result = new List<Review>();
        var type = positive ? "positive" : "negative";

        string cursor = "*";

        while (result.Count < ReviewsPerType)
        {
            var url =
                $"{BaseUrl}/{appId}" +
                $"?json=1" +
                $"&language={Lang}" +
                $"&num_per_page=20" + // больше страницы
                $"&purchase_type=all" +
                $"&review_type={type}" +
                $"&cursor={Uri.EscapeDataString(cursor)}";

            var response =
                await client.GetFromJsonAsync<SteamReviewResponse>(
                    url,
                    token);

            if (response == null ||
                response.Success != 1 ||
                response.Reviews.Count == 0)
            {
                break;
            }

            foreach (var steamReview in response.Reviews)
            {
                result.Add(ConvertToEntity(steamReview, gameId));
            }

            // Отзывы закончились
            if (response.Cursor == cursor)
                break;

            cursor = response.Cursor;
        }

        _logger.LogDebug(
            "Fetched {Count} {Type} reviews for AppId {AppId}",
            result.Count, type, appId);

        return result;
    }

    // =========================
    // MAPPING
    // =========================
    private Review ConvertToEntity(
        SteamReviewDto dto,
        int gameId)
    {
        return new Review
        {
            ExternalId = dto.Recommendationid,
            GameId = gameId,
            Source = ReviewSource.Steam,
            Author = dto.Author?.Personaname,
            Content = dto.Review,
            Recommended = dto.Voted_Up,
            Rating = dto.Voted_Up ? 1f : 0f,
            VotesUp = dto.Votes_Up,
            VotesFunny = dto.Votes_Funny,
            WeightedVoteScore = dto.Weighted_Vote_Score,
            PlaytimeAtReview = dto.Author?.Playtime_At_Review,
            WrittenDuringEarlyAccess = dto.Written_During_Early_Access,
            CreatedAt =
                DateTimeOffset
                    .FromUnixTimeSeconds(dto.Timestamp_Created)
                    .UtcDateTime,
            UpdatedAt =
                DateTimeOffset
                    .FromUnixTimeSeconds(dto.Timestamp_Updated)
                    .UtcDateTime
        };
    }

    // =========================
    // TEST CONNECTION
    // =========================
    public async Task<bool> TestConnectionAsync(
        HttpClient client,
        CancellationToken token)
    {
        try
        {
            var url =
                $"{BaseUrl}/730?json=1&num_per_page=1";

            var response =
                await client.GetAsync(url, token);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}