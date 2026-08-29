using Newtonsoft.Json;
using GReSym.Parser.Enums;
using GReSym.Core.Exceptions;

namespace GReSym.Parser.Models;

[JsonObject(MemberSerialization.OptIn)]
public class ParsingError
{
    [JsonProperty("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    [JsonProperty("identifier")]
    public string Identifier { get; init; } = string.Empty;
    
    [JsonProperty("source")]
    public string Source { get; init; } = string.Empty;
    
    [JsonProperty("message")]
    public string Message { get; init; } = string.Empty;
    
    [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
    public string? Details { get; init; }
    
    [JsonProperty("occurredAt")]
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
    [JsonProperty("stackTrace", NullValueHandling = NullValueHandling.Ignore)]
    public string? StackTrace { get; init; }
    
    [JsonProperty("errorType", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public ParsingErrorType ErrorType { get; init; } = ParsingErrorType.Unknown;
    
    [JsonProperty("canRetry")]
    public bool CanRetry { get; init; }
    
    [JsonProperty("retryCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int RetryCount { get; private set; }
    
    [JsonProperty("metadata")]
    public Dictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    
    [JsonProperty("requestUrl", NullValueHandling = NullValueHandling.Ignore)]
    public string? RequestUrl { get; init; }
    
    [JsonProperty("httpStatusCode", NullValueHandling = NullValueHandling.Ignore)]
    public int? HttpStatusCode { get; init; }
    
    [JsonProperty("executionTime", NullValueHandling = NullValueHandling.Ignore)]
    public TimeSpan? ExecutionTime { get; init; }
    
    [JsonProperty("proxyName", NullValueHandling = NullValueHandling.Ignore)]
    public string? ProxyName { get; init; }
    
    public ParsingError() { }
    
    public ParsingError(Exception exception, string identifier, string source)
    {
        Identifier = identifier ?? string.Empty;
        Source = source ?? string.Empty;
        Message = exception.Message;
        Details = exception.InnerException?.Message;
        StackTrace = exception.StackTrace;
        OccurredAt = DateTime.UtcNow;
        ErrorType = DetermineErrorType(exception);
        CanRetry = DetermineRetryAbility(exception);
        
        // Добавляем дополнительную информацию в метаданные
        Metadata["exceptionType"] = exception.GetType().FullName ?? "Unknown";
        Metadata["exceptionSource"] = exception.Source ?? "Unknown";
        
        if (exception is HttpRequestException httpEx)
        {
            HttpStatusCode = (int?)httpEx.StatusCode;
            Metadata["isHttpError"] = true;
        }
        
        // Определяем CanRetry на основе типа ошибки
        CanRetry = DetermineRetryAbility(exception);
    }
    
    public ParsingError(string identifier, string source, string message)
    {
        Identifier = identifier ?? string.Empty;
        Source = source ?? string.Empty;
        Message = message ?? string.Empty;
        OccurredAt = DateTime.UtcNow;
        ErrorType = DetermineErrorTypeFromMessage(Message);
        CanRetry = DetermineRetryAbilityFromErrorType(ErrorType);
    }
    
    public static ParsingError CreateHttpError(
        string identifier,
        string source,
        string url,
        int statusCode,
        string responseContent = "")
    {
        return new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = GetHttpErrorMessage(statusCode),
            Details = responseContent.Length > 500 
                ? responseContent[..500] + "..." 
                : responseContent,
            RequestUrl = url,
            HttpStatusCode = statusCode,
            ErrorType = ParsingErrorType.Network,
            CanRetry = statusCode != 404 && statusCode != 403 && statusCode != 401 && statusCode != 400,
            Metadata = new Dictionary<string, object>
            {
                ["httpMethod"] = "GET",
                ["responseLength"] = responseContent.Length
            }
        };
    }
    
    public static ParsingError CreateTimeoutError(
        string identifier,
        string source,
        string url,
        TimeSpan timeout)
    {
        return new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = $"Timeout after {timeout.TotalSeconds} seconds",
            RequestUrl = url,
            ErrorType = ParsingErrorType.Timeout,
            CanRetry = true,
            RetryCount = 1,
            Metadata = new Dictionary<string, object>
            {
                ["timeoutSeconds"] = timeout.TotalSeconds,
                ["isTimeout"] = true
            }
        };
    }
    
    public static ParsingError CreateProxyError(
        string identifier,
        string source,
        string proxyUrl,
        string errorMessage)
    {
        return new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = $"Proxy error: {errorMessage}",
            ProxyName = proxyUrl,
            ErrorType = ParsingErrorType.Proxy,
            CanRetry = true,
            Metadata = new Dictionary<string, object>
            {
                ["proxyUrl"] = proxyUrl,
                ["proxyFailed"] = true
            }
        };
    }
    
    public static ParsingError CreateValidationError(
        string identifier,
        string source,
        string validationMessage,
        string fieldName = "")
    {
        return new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = $"Validation error: {validationMessage}",
            ErrorType = ParsingErrorType.Validation,
            CanRetry = false,
            Metadata = string.IsNullOrEmpty(fieldName) 
                ? new Dictionary<string, object>()
                : new Dictionary<string, object> { ["field"] = fieldName }
        };
    }
    
    public static ParsingError CreateDatabaseError(
        string identifier,
        string source,
        Exception dbException)
    {
        var error = new ParsingError(dbException, identifier, source)
        {
            ErrorType = ParsingErrorType.Database,
            CanRetry = true
        };
        
        error.Metadata["databaseError"] = true;
        error.Metadata["dbException"] = dbException.GetType().Name;
        
        return error;
    }
    
    public static ParsingError CreateRateLimitError(
        string identifier,
        string source,
        string url,
        TimeSpan retryAfter)
    {
        return new ParsingError
        {
            Identifier = identifier,
            Source = source,
            Message = $"Rate limit exceeded. Retry after {retryAfter.TotalSeconds} seconds",
            RequestUrl = url,
            ErrorType = ParsingErrorType.RateLimit,
            CanRetry = true,
            Metadata = new Dictionary<string, object>
            {
                ["rateLimited"] = true,
                ["retryAfterSeconds"] = retryAfter.TotalSeconds,
                ["occurredAt"] = DateTime.UtcNow
            }
        };
    }
    
    public ParsingError WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }
    
    public ParsingError IncrementRetry()
    {
        RetryCount++;
        return this;
    }
    
    public bool ShouldRetry(int maxRetries = 3)
    {
        return CanRetry && RetryCount < maxRetries;
    }
    
    public override string ToString()
    {
        return $"[{OccurredAt:HH:mm:ss}] {Source} | {Identifier} | {ErrorType}: {Message}";
    }
    
    public string ToJson(bool indented = true)
    {
        return JsonConvert.SerializeObject(this, indented 
            ? Formatting.Indented 
            : Formatting.None);
    }
    
    public static ParsingError? FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ParsingError>(json);
    }
    
    private static ParsingErrorType DetermineErrorType(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => httpEx.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => ParsingErrorType.NotFound,
                System.Net.HttpStatusCode.Forbidden => ParsingErrorType.AccessDenied,
                System.Net.HttpStatusCode.Unauthorized => ParsingErrorType.AccessDenied,
                System.Net.HttpStatusCode.TooManyRequests => ParsingErrorType.RateLimit,
                System.Net.HttpStatusCode.RequestTimeout => ParsingErrorType.Timeout,
                System.Net.HttpStatusCode.ServiceUnavailable => ParsingErrorType.ServiceUnavailable,
                System.Net.HttpStatusCode.GatewayTimeout => ParsingErrorType.Timeout,
                _ => ParsingErrorType.Network
            },
            GameNotFoundException => ParsingErrorType.NotFound,
            TimeoutException => ParsingErrorType.Timeout,
            JsonException => ParsingErrorType.Parsing,
            InvalidOperationException => ParsingErrorType.Validation,
            ArgumentException => ParsingErrorType.Validation,
            OperationCanceledException => ParsingErrorType.Cancellation,
            _ => ParsingErrorType.Unknown
        };
    }
    
    private static ParsingErrorType DetermineErrorTypeFromMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return ParsingErrorType.Unknown;
            
        var msg = message.ToLowerInvariant();
        
        if (msg.Contains("timeout") || msg.Contains("timed out"))
            return ParsingErrorType.Timeout;
            
        if (msg.Contains("not found") || msg.Contains("404"))
            return ParsingErrorType.NotFound;
            
        if (msg.Contains("rate limit") || msg.Contains("too many requests") || msg.Contains("429"))
            return ParsingErrorType.RateLimit;
            
        if (msg.Contains("forbidden") || msg.Contains("403") || msg.Contains("unauthorized") || msg.Contains("401"))
            return ParsingErrorType.AccessDenied;
            
        if (msg.Contains("json") || msg.Contains("parse") || msg.Contains("deserialize"))
            return ParsingErrorType.Parsing;
            
        if (msg.Contains("validation") || msg.Contains("invalid") || msg.Contains("required"))
            return ParsingErrorType.Validation;
            
        if (msg.Contains("database") || msg.Contains("sql") || msg.Contains("constraint"))
            return ParsingErrorType.Database;
            
        if (msg.Contains("proxy"))
            return ParsingErrorType.Proxy;
            
        return ParsingErrorType.Unknown;
    }
    
    private static bool DetermineRetryAbility(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => httpEx.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => false,
                System.Net.HttpStatusCode.Forbidden => false,
                System.Net.HttpStatusCode.Unauthorized => false,
                System.Net.HttpStatusCode.BadRequest => false,
                _ => true
            },
            TimeoutException => true,
            JsonException => false,
            InvalidOperationException => false,
            OperationCanceledException => true,
            _ => true
        };
    }
    
    private static bool DetermineRetryAbilityFromErrorType(ParsingErrorType errorType)
    {
        return errorType switch
        {
            ParsingErrorType.NotFound => false,
            ParsingErrorType.AccessDenied => false,
            ParsingErrorType.Validation => false,
            ParsingErrorType.Parsing => false,
            ParsingErrorType.Timeout => true,
            ParsingErrorType.RateLimit => true,
            ParsingErrorType.Proxy => true,
            ParsingErrorType.Database => true,
            ParsingErrorType.Network => true,
            ParsingErrorType.ServiceUnavailable => true,
            ParsingErrorType.Cancellation => true,
            _ => true // По умолчанию разрешаем повтор
        };
    }
    
    private static string GetHttpErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => $"HTTP Error {statusCode}"
        };
    }
}