using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

namespace GReSym.Infrastructure.Base
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public IGameRepository Games { get; }
        public IUserRepository Users { get; }
        public IReviewRepository Reviews { get; }
        public ITagRepository Tags { get; }
        public IGameTagRepository GameTags { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Games = new GameRepository(context);
            Users = new UserRepository(context);
            Reviews = new ReviewRepository(context);
            Tags = new TagRepository(context);
            GameTags = new GameTagRepository(context);
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                foreach (var entry in ex.Entries)
                {
                    // stop EF from retrying this entity
                    entry.State = EntityState.Detached;
                }

                // try again without duplicates
                return await _context.SaveChangesAsync();
            }
        }

        private static bool IsDuplicateKey(DbUpdateException ex)
        {
            return ex.InnerException is MySqlException mysql &&
                mysql.Number == 1062;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}