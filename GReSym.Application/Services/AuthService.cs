using System.Security.Authentication;
using GReSym.Application.DTO.Auth;
using GReSym.Application.Interfaces;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwt,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwt = jwt;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        string password = request.Password.Trim();
        string email = request.Email.Trim().ToLower();

        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
            throw new InvalidCredentialException("User doesn't exist.");

        bool passwordValid = BCrypt.Net.BCrypt.Verify(
            password,
            user.PasswordHash);

        if (!passwordValid)
            throw new InvalidCredentialException("Invalid password.");

        var token = _jwt.GenerateToken(
            user.Id,
            user.Email,
            user.IsAdmin);

        return new AuthResponseDto
        {
            Token = token
        };
    }

    private bool IsUniqueEmailViolation(DbUpdateException ex)
    {
        return ex.InnerException != null 
            && ex.InnerException.Message.Contains("Duplicate entry"); // MySQL
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        string password = request.Password.Trim();
        string email = request.Email.Trim().ToLower();
        
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if (existingUser != null)
            throw new EmailOccupiedException();

        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hash,
            IsAdmin = false
        };

        await _userRepository.AddAsync(user);
        
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueEmailViolation(ex))
        {
            throw new EmailOccupiedException();
        }

        var token = _jwt.GenerateToken(
            user.Id,
            user.Email,
            user.IsAdmin);

        return new AuthResponseDto
        {
            Token = token
        };
    }
}