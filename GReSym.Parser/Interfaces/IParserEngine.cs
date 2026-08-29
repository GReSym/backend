using GReSym.Parser.Models;

namespace GReSym.Parser.Interfaces;

public interface IParserEngine
{
    Task<ParsingResult> ParseAsync(ParserContext context, CancellationToken cancellationToken);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    ParserState GetCurrentState();

    event EventHandler<ParsingProgressEventArgs> ProgressChanged;
    IEnumerable<string> GetCheckpoints();
}