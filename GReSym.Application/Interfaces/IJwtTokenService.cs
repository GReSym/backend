namespace GReSym.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string email, bool isAdmin);
}