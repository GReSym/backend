using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Infrastructure.Base;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            Update(user);
        }
    }

    public async Task<IEnumerable<UserGameRate>> GetUserRatingsAsync(int userId)
    {
        var user = await _dbSet
            .Include(u => u.UserGameRates)
                .ThenInclude(ugr => ugr.Game)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.UserGameRates ?? Enumerable.Empty<UserGameRate>();
    }

    public async Task<UserGameRate?> GetUserGameRatingAsync(int userId, int gameId)
    {
        var user = await _dbSet
            .Include(u => u.UserGameRates)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.UserGameRates.FirstOrDefault(ugr => ugr.GameId == gameId);
    }
}