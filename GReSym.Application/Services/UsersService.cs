using GReSym.Application.DTO.Users;
using GReSym.Application.Interfaces;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;

namespace GReSym.Application.Services;

public class UsersService : IUsersService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UsersService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserInfoResponseDto> GetUserInfo(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new UserNotFoundException(userId);

        return new UserInfoResponseDto
        {
            Id = user.Id,
            Name = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };
    }

    public async Task<UserInfoResponseDto> SetUsername(int userId, string name)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        name = name.Trim();

        if (user == null)
            throw new UserNotFoundException(userId);

        if (name.Length < 4)
            throw new UsernameShortException();

        user.Username = name;
        await _unitOfWork.SaveChangesAsync();

        return new UserInfoResponseDto
        {
            Id = user.Id,
            Name = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };
    }
}