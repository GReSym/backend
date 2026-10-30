using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Base;
using GReSym.Infrastructure.Data;
using GReSym.Infrastructure.Messaging;
using GReSym.Infrastructure.Ml;
using GReSym.Application.Interfaces;
using GReSym.Application.Services;

using Microsoft.EntityFrameworkCore;

namespace GReSym.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Fixed version: AutoDetect opens a connection during DI setup and fails if the DB isn't up yet
            options.UseMySql(
                connectionString,
                new MariaDbServerVersion(new Version(11, 8)));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserGameRateRepository, UserGameRateRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IUserRecommendationRepository, UserRecommendationRepository>();

        services.AddScoped<IEvaluationRepository, EvaluationRepository>();

        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IGameVectorizationQueue, RabbitMqVectorizationQueue>();
        services.AddSingleton<IEvaluationQueue, RabbitMqEvaluationQueue>();
        services.AddHostedService<EvaluationResultsConsumer>();
        services.AddSingleton<IRecommendationQueue, RabbitMqRecommendationQueue>();
        services.AddHostedService<RecommendationResultsConsumer>();

        var mlSettings = configuration.GetSection("Ml").Get<MlServiceSettings>() ?? new MlServiceSettings();
        services.AddHttpClient<IGameVectorIndex, MlGameVectorIndex>(client =>
        {
            // Trailing slash so relative paths ("vectorization/stale") append to the base path
            client.BaseAddress = new Uri(mlSettings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(mlSettings.ScanTimeoutSeconds);
        });
        services.AddHttpClient<IGameSearchIndex, MlGameSearchIndex>(client =>
        {
            client.BaseAddress = new Uri(mlSettings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(mlSettings.SearchTimeoutSeconds);
        });

        return services;
    }

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IGamesService, GamesService>();
        services.AddScoped<IRatingsService, RatingsService>();
        services.AddScoped<IRecommendationsService, RecommendationsService>();
        services.AddScoped<RecommendationResultHandler>();
        services.AddScoped<IRecommendationResultHandler>(sp => sp.GetRequiredService<RecommendationResultHandler>());
        services.AddScoped<IEvaluationService, EvaluationService>();

        return services;
    }
}