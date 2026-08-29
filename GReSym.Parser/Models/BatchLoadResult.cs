using Newtonsoft.Json;

namespace GReSym.Parser.Models;

[JsonObject(MemberSerialization.OptIn)]
public class BatchLoadResult : OperationResult
{
    [JsonProperty("itemResults")]
    public List<LoadResult> ItemResults { get; set; } = new();
    
    [JsonProperty("totalCount")]
    public int TotalCount => ItemResults.Count;
    
    [JsonProperty("successCount")]
    public int SuccessCount => ItemResults.Count(r => r.Success);
    
    [JsonProperty("failureCount")]
    public int FailureCount => ItemResults.Count(r => !r.Success);
    
    [JsonProperty("successRate")]
    public double SuccessRate => TotalCount > 0 ? SuccessCount * 100.0 / TotalCount : 0;
    
    [JsonIgnore]
    public List<LoadResult> SuccessfulItems => ItemResults.Where(r => r.Success).ToList();
    
    [JsonIgnore]
    public List<LoadResult> FailedItems => ItemResults.Where(r => !r.Success).ToList();
    
    private BatchLoadResult(bool success, string? error = null) 
        : base(success, error) { }
    
    public static BatchLoadResult FromItems(IEnumerable<LoadResult> items, string? error = null)
    {
        var itemList = items.ToList();
        var success = itemList.All(r => r.Success) && string.IsNullOrEmpty(error);
        
        return new BatchLoadResult(success, error)
        {
            ItemResults = itemList
        };
    }
    
    public static BatchLoadResult SingleSuccess(LoadResult result)
    {
        return new BatchLoadResult(true)
        {
            ItemResults = new List<LoadResult> { result }
        };
    }
    
    public static BatchLoadResult SingleFailure(LoadResult result)
    {
        return new BatchLoadResult(false, result.Error)
        {
            ItemResults = new List<LoadResult> { result }
        };
    }
    
    public BatchLoadResult AddItemResult(LoadResult result)
    {
        ItemResults.Add(result);
        
        // Пересчитываем общий успех
        if (!result.Success && Success)
        {
            Success = false;
            Error = "Some items failed to load";
        }
        
        return this;
    }
    
    public Dictionary<LoadOperationType, int> GetOperationStatistics()
    {
        return ItemResults
            .Where(r => r.Success)
            .GroupBy(r => r.Operation)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}