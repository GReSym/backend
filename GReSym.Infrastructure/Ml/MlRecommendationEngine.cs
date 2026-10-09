using System.Net.Http.Json;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;

namespace GReSym.Infrastructure.Ml;

/// <summary>Calls the Python ML service (ml/src/ml/api.py). Register as a typed HttpClient.</summary>
public class MlRecommendationEngine : IRecommendationEngine
{
    private readonly HttpClient _http;

    public MlRecommendationEngine(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<ScoredGame>> RecommendAsync(
        IReadOnlyCollection<GameRatingSignal> ratings,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var request = new RecommendationsRequest(
            ratings.Select(r => new RatingItem(r.GameId, r.Rating, r.Recommend)).ToList(),
            limit);

        try
        {
            using var response = await _http.PostAsJsonAsync("recommendations", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new RecommendationEngineException(
                    $"ML service returned {(int)response.StatusCode}: {Truncate(body, 500)}");
            }

            var result = await response.Content.ReadFromJsonAsync<RecommendationsResponse>(cancellationToken)
                ?? throw new RecommendationEngineException("ML service returned an empty body");

            return result.Items.Select(i => new ScoredGame(i.GameId, i.Score)).ToList();
        }
        catch (HttpRequestException e)
        {
            throw new RecommendationEngineException("ML service is unavailable", e);
        }
        catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecommendationEngineException("ML service timed out", e);
        }
        catch (System.Text.Json.JsonException e)
        {
            throw new RecommendationEngineException("ML service returned invalid JSON", e);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    // Wire format of the ML service (camelCase JSON)
    private record RatingItem(int GameId, int? Rating, bool? Recommend);
    private record RecommendationsRequest(List<RatingItem> Ratings, int Limit);
    private record ScoredItem(int GameId, double Score);
    private record RecommendationsResponse(List<ScoredItem> Items, int UsedRatings);
}
