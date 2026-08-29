using GReSym.Application.DTO.Users;

namespace GReSym.Application.Interfaces;

public interface IUsersService
{
    Task<UserInfoResponseDto> GetUserInfo(int userId);
    Task<UserInfoResponseDto> SetUsername(int userId, string name);
}