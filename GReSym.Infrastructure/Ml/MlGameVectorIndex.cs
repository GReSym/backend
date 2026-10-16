using System.Net.Http.Json;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using GReSym.Core.Models;

namespace GReSym.Infrastructure.Ml;

/// <summary>Vector state from the Python ML service (GET /vectorization/stale). Register as a typed HttpClient.</summary>
public class MlGameVectorIndex : IGameVectorIndex
{
    private readonly HttpClient _http;

    public MlGameVectorIndex(HttpClient http)
    {
        _http = http;
    }

    public async Task<StaleGamesReport> GetStaleGamesAsync(
        IReadOnlyCollection<string> reasons,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = string.Join("&",
            reasons.Select(r => $"reason={Uri.EscapeDataString(r)}")
                .Append($"limit={limit}")
                .Append($"offset={offset}"));

        try
        {
            using var response = await _http.GetAsync($"vectorization/stale?{query}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new MlServiceException(
                    $"ML service returned {(int)response.StatusCode}: {(body.Length <= 500 ? body : body[..500])}");
            }

            var result = await response.Content.ReadFromJsonAsync<StaleGamesResponse>(cancellationToken)
                ?? throw new MlServiceException("ML service returned an empty body");

            return new StaleGamesReport(
                result.Collection,
                result.TextVersion,
                result.Vectors,
                result.Games,
                result.Counts,
                result.Total,
                result.Items.Select(i => new StaleGame(
                    i.GameId,
                    i.Score,
                    i.Reasons,
                    i.VectorizedAt is { } at ? DateTimeOffset.FromUnixTimeSeconds(at).UtcDateTime : null,
                    i.Details)).ToList());
        }
        catch (HttpRequestException e)
        {
            throw new MlServiceException("ML service is unavailable", e);
        }
        catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MlServiceException("ML service timed out", e);
        }
        catch (System.Text.Json.JsonException e)
        {
            throw new MlServiceException("ML service returned invalid JSON", e);
        }
    }

    // Wire format of the ML service (camelCase JSON)
    private record StaleItem(
        int GameId,
        double Score,
        List<string> Reasons,
        long? VectorizedAt,
        Dictionary<string, double?> Details);

    private record StaleGamesResponse(
        string Collection,
        int TextVersion,
        int Vectors,
        int Games,
        Dictionary<string, int> Counts,
        int Total,
        List<StaleItem> Items);
}
