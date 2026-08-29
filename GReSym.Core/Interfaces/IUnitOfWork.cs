namespace GReSym.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGameRepository Games { get; }
    IUserRepository Users { get; }
    IReviewRepository Reviews { get; }
    ITagRepository Tags { get; }
    IGameTagRepository GameTags { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}