using System.Net.Http.Json;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Infrastructure.Ml;

/// <summary>Game search of the Python ML service (GET /search). Register as a typed HttpClient.</summary>
public class MlGameSearchIndex : IGameSearchIndex
{
    private readonly HttpClient _http;

    public MlGameSearchIndex(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<int>> SearchAsync(
        string query,
        bool exact,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var url = $"search?q={Uri.EscapeDataString(query)}&exact={(exact ? "true" : "false")}&limit={limit}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new MlServiceException(
                    $"ML service returned {(int)response.StatusCode}: {(body.Length <= 500 ? body : body[..500])}");
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken)
                ?? throw new MlServiceException("ML service returned an empty body");

            return result.Items.Select(i => i.GameId).ToList();
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
    private record SearchItem(int GameId, double Score);

    private record SearchResponse(List<SearchItem> Items, string Mode);
}
