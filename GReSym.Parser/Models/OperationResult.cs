using Newtonsoft.Json;

namespace GReSym.Parser.Models;

[JsonObject(MemberSerialization.OptIn)]
public abstract class OperationResult
{
    [JsonProperty("success")]
    public bool Success { get; protected set; }
    
    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public string? Error { get; protected set; }
    
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; protected set; } = DateTime.UtcNow;
    
    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    protected OperationResult(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }
    
    public OperationResult WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
    
    public OperationResult WithMetadata(Dictionary<string, object> metadata)
    {
        foreach (var kvp in metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }
        return this;
    }
    
    public string ToJson(bool indented = true)
    {
        return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
    }
    public static T? FromJson<T>(string json) where T : OperationResult
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}