namespace GReSym.Core.ValueObjects;

public record SystemRequirements
{
    public string? Minimum { get; init; }
    public string? Recommended { get; init; }
}