using GReSym.Core.Entities.UserInfo;

namespace GReSym.Core.Interfaces;
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task UpdatePasswordAsync(int userId, string newPasswordHash);

    // Для рекомендаций
    Task<IEnumerable<UserGameRate>> GetUserRatingsAsync(int userId);
    Task<UserGameRate?> GetUserGameRatingAsync(int userId, int gameId);
}