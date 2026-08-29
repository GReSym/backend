namespace GReSym.Parser.Interfaces;

public interface IDataExtractor<T> where T : class
{
    string SourceName { get; }
    Task<IEnumerable<T>> ExtractBatchAsync(IEnumerable<string> identifiers, HttpClient client, CancellationToken cancellationToken, string key="");
    Task<bool> TestConnectionAsync(HttpClient client, CancellationToken cancellationToken);
}