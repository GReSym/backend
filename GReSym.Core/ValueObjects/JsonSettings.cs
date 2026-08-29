using Newtonsoft.Json;

namespace GReSym.Core.ValueObjects;

public class JsonSettings
{
    // Конструктор без параметров для EF Core
    protected JsonSettings()
    {
        _json = "{}";
    }
    
    private string _json;
    
    // Основной конструктор для использования в коде
    public JsonSettings(string json)
    {
        _json = json ?? "{}";
    }
    
    public T? GetValue<T>(string key)
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(_json);
        if (dict != null && dict.TryGetValue(key, out var value))
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
        }
        return default;
    }
    
    public void SetValue<T>(string key, T value)
    {
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(_json) 
                  ?? new Dictionary<string, object>();
        dict[key] = value!;
        _json = JsonConvert.SerializeObject(dict);
    }
    
    public string ToJson() => _json;
    
    public static JsonSettings FromJson(string json) => new(json);
    public static JsonSettings Empty() => new("{}");
}