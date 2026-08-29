using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GReSym.Parser.Models;

[JsonObject(MemberSerialization.OptIn)]
public class LoadResult : OperationResult
{
    [JsonProperty("entityId", NullValueHandling = NullValueHandling.Ignore)]
    public string? EntityId { get; set; }
    
    [JsonProperty("operation")]
    [JsonConverter(typeof(StringEnumConverter))]
    public LoadOperationType Operation { get; set; }
    
    [JsonProperty("entityType", NullValueHandling = NullValueHandling.Ignore)]
    public string? EntityType { get; set; }
    
    [JsonProperty("affectedRows", NullValueHandling = NullValueHandling.Ignore)]
    public int? AffectedRows { get; set; }
    
    private LoadResult(bool success, string? error = null) 
        : base(success, error) { }
    
  
    public static LoadResult AsSuccess(string entityId, LoadOperationType operation, string entityType)
    {
        return new LoadResult(true)
        {
            EntityId = entityId,
            Operation = operation,
            EntityType = entityType
        };
    }
    
    public static LoadResult AsSuccess(LoadOperationType operation, string entityType, int affectedRows = 1)
    {
        return new LoadResult(true)
        {
            Operation = operation,
            EntityType = entityType,
            AffectedRows = affectedRows
        };
    }
    
    public static LoadResult AsFailure(string error, string? entityId = null)
    {
        return new LoadResult(false, error)
        {
            EntityId = entityId
        };
    }
    
    public static LoadResult AsSkipped(string reason, string? entityType = null)
    {
        return new LoadResult(true, reason)
        {
            Operation = LoadOperationType.Skip,
            EntityType = entityType
        };
    }
    
    public LoadResult WithEntityId(string id)
    {
        EntityId = id;
        return this;
    }
    
    public LoadResult WithEntityType(string type)
    {
        EntityType = type;
        return this;
    }
    public LoadResult WithAffectedRows(int rows)
    {
        AffectedRows = rows;
        return this;
    }
}