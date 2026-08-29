using GReSym.Parser.Models;

namespace GReSym.Parser.Interfaces;

public interface IParserStateManager
{
    Task SaveStateAsync(ParserState state, string checkpointName);
    Task<ParserState> LoadStateAsync(string checkpointName);
    Task ClearStateAsync(string checkpointName);
    IEnumerable<string> GetAvailableCheckpoints();
}