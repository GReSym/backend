using System.Net.Http.Json;
using System.Xml;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Entities.SourceData;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Parser.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace GReSym.Parser.ETL;

public class SteamApiEnvelope<T>
{
    [JsonPropertyName("response")]
    public T Response { get; set; } = default!;
}

public class SteamReviewResponse
{
    [JsonPropertyName("query_summary")] public QuerySummary Query_Summary { get; set; } = default!;
    [JsonPropertyName("reviews")]       public List<SteamReviewDto> Reviews { get; set; } = [];
    [JsonPropertyName("cursor")]        public string Cursor { get; set; } = "*";
    [JsonPropertyName("total_matching")] public int? Total_Matching { get; set; }      // NEW
    [JsonPropertyName("day_range_used")] public int? Day_Range_Used { get; set; }      // NEW
}

public class QuerySummary
{
    [JsonPropertyName("num_reviews")]      public int Num_Reviews { get; set; }
    [JsonPropertyName("total_positive")]   public int? Total_Positive { get; set; }
    [JsonPropertyName("total_negative")]   public int? Total_Negative { get; set; }
    [JsonPropertyName("total_reviews")]    public int? Total_Reviews { get; set; }   // new field
}

public class SteamReviewDto
{
    [JsonPropertyName("recommendationid")]         public string Recommendationid { get; set; } = "";
    [JsonPropertyName("author")]                   public SteamAuthor Author { get; set; } = default!;
    [JsonPropertyName("language")]                 public string Language { get; set; } = "";        // NEW
    [JsonPropertyName("review")]                   public string Review { get; set; } = "";
    [JsonPropertyName("voted_up")]                 public bool Voted_Up { get; set; }
    [JsonPropertyName("timestamp_created")]        public long Timestamp_Created { get; set; }
    [JsonPropertyName("timestamp_updated")]        public long Timestamp_Updated { get; set; }
    [JsonPropertyName("votes_up")]                 public int Votes_Up { get; set; }
    [JsonPropertyName("votes_funny")]              public int Votes_Funny { get; set; }
    [JsonPropertyName("weighted_vote_score")]      public double Weighted_Vote_Score { get; set; }
    [JsonPropertyName("comment_count")]            public int Comment_Count { get; set; }             // NEW
    [JsonPropertyName("written_during_early_access")] public bool Written_During_Early_Access { get; set; }
    [JsonPropertyName("primarily_steam_deck")]     public bool Primarily_Steam_Deck { get; set; }      // NEW
    [JsonPropertyName("steam_purchase")]           public bool Steam_Purchase { get; set; }            // NEW
    [JsonPropertyName("refunded")]                 public bool Refunded { get; set; }                  // NEW
}

public class SteamAuthor
{
    [JsonPropertyName("steamid")]              public string Steamid { get; set; } = "";
    [JsonPropertyName("num_reviews")]          public int? Num_Reviews { get; set; }           // NEW
    [JsonPropertyName("playtime_forever")]     public int? Playtime_Forever { get; set; }      // NEW
    [JsonPropertyName("playtime_at_review")]   public int? Playtime_At_Review { get; set; }
    [JsonPropertyName("playtime_last_two_weeks")] public int? Playtime_Last_Two_Weeks { get; set; } // NEW
    [JsonPropertyName("deck_playtime_at_review")] public int? Deck_Playtime_At_Review { get; set; } // NEW
    [JsonPropertyName("last_played")]          public long? Last_Played { get; set; }          // NEW
}

public class SteamReviewExtractor : IDataExtractor<Review>
{
    private readonly ILogger<SteamReviewExtractor> _logger;
    private readonly IUnitOfWork _uow;

    private const string BaseUrl =
        "https://api.steampowered.com/IUserReviewsService/GetAppReviews/v1/";

    private const int ReviewsPerType = 5;
    private const int PageSize = 20;

    /// <summary>Upper bound per game: one page of positive and one of negative reviews.</summary>
    public const int MaxReviewsPerGame = 2 * PageSize;

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
                $"{BaseUrl}?appid={steamSource.SteamAppId}" +
                $"&purchase_type=all" +
                $"&num_per_page=1";

            var envelope = await client.GetFromJsonAsync<SteamApiEnvelope<SteamReviewResponse>>(url, token);
            var response = envelope?.Response;

            if (response?.Query_Summary?.Total_Positive == null)
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
        int appId,
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
                $"{BaseUrl}?appid={appId}" +
                $"&languages[0]=russian" +
                $"&languages[1]=english" +
                $"&num_per_page={PageSize}" + // больше страницы
                $"&purchase_type=all" +
                $"&review_type={type}" +
                $"&cursor={Uri.EscapeDataString(cursor)}";

            var envelope = await client.GetFromJsonAsync<SteamApiEnvelope<SteamReviewResponse>>(url, token);
            var response = envelope?.Response;

            if (response == null || response.Reviews.Count == 0)
                break;

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
            Author = dto.Author?.Steamid,
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
                $"{BaseUrl}?appid=730&num_per_page=1";

            var envelope = await client.GetFromJsonAsync<SteamApiEnvelope<SteamReviewResponse>>(url, token);
            var response = envelope?.Response;

            return response != null;
        }
        catch
        {
            return false;
        }
    }
}
