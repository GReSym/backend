using System.Text.Json;

namespace GReSym.Core.Common;

public static class Helpers
{
    public static string SerializeToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
    
    public static T? DeserializeFromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    
    public static DateOnly? ParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;
            
        return DateOnly.TryParse(dateString, out var date) ? date : null;
    }
}