using GReSym.Core.Entities.Feedback;
using GReSym.Core.Enums;
using GReSym.Core.Interfaces;
using GReSym.Parser.Interfaces;
using GReSym.Parser.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace GReSym.Parser.ETL;

public class ReviewLoader : IDataLoader<Review>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewLoader> _logger;

    public ReviewLoader(
        IUnitOfWork unitOfWork,
        ILogger<ReviewLoader> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ====================================================
    // SINGLE LOAD
    // ====================================================
    public async Task<LoadResult> LoadAsync(
        Review review,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Loading review for GameId {GameId}",
                review.GameId);

            var operation =
                await ProcessReviewAsync(review, cancellationToken);

            var affectedRows =
                await _unitOfWork.SaveChangesAsync();

            return LoadResult
                .AsSuccess(operation, nameof(Review), affectedRows)
                .WithEntityId(review.ExternalId ?? "unknown");
        }
        catch (DbUpdateException ex)
        {   
            _logger.LogError(ex,
                "DB error loading review");

            return LoadResult.AsFailure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error loading review");

            return LoadResult.AsFailure(ex.Message);
        }
    }

    // ====================================================
    // BATCH LOAD
    // ====================================================
    public async Task<BatchLoadResult> LoadBatchAsync(
        IEnumerable<Review> reviews,
        CancellationToken cancellationToken)
    {
        var results = new List<LoadResult>();

        try
        {
            var reviewList = reviews.ToList();

            _logger.LogInformation(
                "Batch loading {Count} reviews",
                reviewList.Count);

            await _unitOfWork.BeginTransactionAsync();

            foreach (var review in reviewList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    results.Add(
                        LoadResult.AsFailure("Cancelled"));
                    break;
                }

                try
                {
                    var operation =
                        await ProcessReviewAsync(
                            review,
                            cancellationToken);

                    results.Add(
                        LoadResult.AsSuccess(
                                operation,
                                nameof(Review))
                            .WithEntityId(
                                review.ExternalId ?? "unknown"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed review load");

                    results.Add(
                        LoadResult.AsFailure(
                            ex.Message,
                            review.ExternalId));
                }
            }

            var affectedRows =
                await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            var batch =
                BatchLoadResult.FromItems(results);

            var avg =
                results.Count > 0
                    ? affectedRows / results.Count
                    : 0;

            batch.ItemResults
                .ForEach(r => r.WithAffectedRows(avg));

            _logger.LogInformation(
                "Batch load completed: {Success}/{Total}",
                batch.SuccessCount,
                batch.TotalCount);

            return batch;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            _logger.LogError(ex,
                "Critical batch error");

            return BatchLoadResult.FromItems(
                results,
                ex.Message);
        }
    }

    // ====================================================
    // CORE UPSERT LOGIC
    // ====================================================
    private async Task<LoadOperationType> ProcessReviewAsync(
        Review review,
        CancellationToken token)
    {
        if (review.GameId == null ||
            string.IsNullOrEmpty(review.ExternalId))
            return LoadOperationType.Skip;

        review.CreatedAt ??= DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Reviews.UpsertAsync(review);

        return LoadOperationType.Insert;
    }
}