namespace GReSym.Parser.Enums;

public enum ParsingErrorType
{
    Unknown,
    Network,
    Timeout,
    NotFound,
    RateLimit,
    AccessDenied,
    Parsing,
    Validation,
    Database,
    Proxy,
    ServiceUnavailable,
    Cancellation
}