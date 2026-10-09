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
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserGameRateRepository, UserGameRateRepository>();

        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqVectorizationQueue>();
        services.AddSingleton<IGameVectorizationQueue>(sp => sp.GetRequiredService<RabbitMqVectorizationQueue>());

        var mlSettings = configuration.GetSection("Ml").Get<MlServiceSettings>() ?? new MlServiceSettings();
        services.AddHttpClient<IRecommendationEngine, MlRecommendationEngine>(client =>
        {
            // Trailing slash so relative paths ("recommendations") append to the base path
            client.BaseAddress = new Uri(mlSettings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(mlSettings.TimeoutSeconds);
        });

        return services;
    }

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IGamesService, GamesService>();
        services.AddScoped<IRatingsService, RatingsService>();
        services.AddScoped<IRecommendationsService, RecommendationsService>();

        return services;
    }
}