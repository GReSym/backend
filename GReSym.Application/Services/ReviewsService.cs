using System.Security.Authentication;
using GReSym.Application.DTO.Auth;
using GReSym.Application.DTO.Reviews;
using GReSym.Application.Interfaces;
using GReSym.Core.Entities.UserInfo;
using GReSym.Core.Exceptions;
using GReSym.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GReSym.Application.Services;

public class ReviewsService : IReviewsService
{
    private readonly IUserRepository _userRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewsService(
        IUserRepository userRepository,
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GameReviewsListDto> GetGameReviews(int gameId, GameReviewsListRequestDto request)
    {
        throw new NotImplementedException();
    }

    public async Task<GameReviewsListDto> GetUserReviews(int userId, GameReviewsListRequestDto request)
    {
        throw new NotImplementedException();
    }

    public async Task<GameReviewDto> UpdateUserReview(int gameId, int userId, AddReviewRequestDto request)
    {
        throw new NotImplementedException();
    }
}