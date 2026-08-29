using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using System.Collections.Concurrent;

namespace GReSym.Parser.Services;

public class InMemoryParserStateManager : IParserStateManager
{
    private readonly ConcurrentDictionary<string, ParserState> _states = new();

    public Task<ParserState?> LoadStateAsync(string checkpointName)
    {
        _states.TryGetValue(checkpointName, out var state);
        return Task.FromResult(state);
    }

    public Task SaveStateAsync(ParserState state, string checkpointName)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        if (string.IsNullOrEmpty(checkpointName))
            throw new ArgumentException("Checkpoint name cannot be null or empty", nameof(checkpointName));

        _states[checkpointName] = state;
        return Task.CompletedTask;
    }

    public Task ClearStateAsync(string checkpointName)
    {
        _states.TryRemove(checkpointName, out _);
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAvailableCheckpoints()
    {
        return _states.Keys;
    }
}