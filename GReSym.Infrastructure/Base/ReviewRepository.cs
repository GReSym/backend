using GReSym.Core.Common;
using GReSym.Core.Entities.Feedback;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace GReSym.Infrastructure.Base;

public class ReviewRepository : RepositoryBase<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetByGameIdAsync(int gameId, int page = 1, int pageSize = Constants.Api.DefaultPageSize)
    {
        return await _dbSet
            .Where(r => r.GameId == gameId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetBySourceAsync(ReviewSource source, int page = 1, int pageSize = Constants.Api.DefaultPageSize)
    {
        return await _dbSet
            .Where(r => r.Source == source)
            .Include(r => r.Game)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByGameAsync(int gameId)
    {
        return await _dbSet.CountAsync(r => r.GameId == gameId);
    }

    public async Task UpsertAsync(Review review)
    {
        try
        {
            var sql = @"
            INSERT INTO reviews
            (external_id, game_id, content, rating, recommended, author, source, created_at, updated_at)
            VALUES
            (@external_id, @game_id, @content, @rating, @recommended, @author, @source, @created_at, @updated_at)
            ON DUPLICATE KEY UPDATE
                content = VALUES(content),
                rating = VALUES(rating),
                recommended = VALUES(recommended),
                author = VALUES(author),
                source = VALUES(source),
                updated_at = VALUES(updated_at);";

            await _context.Database.ExecuteSqlRawAsync(
                sql,
                new MySqlParameter("@external_id", review.ExternalId),
                new MySqlParameter("@game_id", review.GameId),
                new MySqlParameter("@content", review.Content),
                new MySqlParameter("@rating", review.Rating),
                new MySqlParameter("@recommended", review.Recommended),
                new MySqlParameter("@author", review.Author),
                new MySqlParameter("@source", review.Source),
                new MySqlParameter("@created_at", review.CreatedAt),
                new MySqlParameter("@updated_at", review.UpdatedAt)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}