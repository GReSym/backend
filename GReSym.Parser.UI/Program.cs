using System;
using Gtk;
using GReSym.Core.Interfaces;
using GReSym.Infrastructure.Base;
using GReSym.Infrastructure.Data;
using GReSym.Parser.ETL;
using GReSym.Parser.Interfaces;
using GReSym.Parser.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using GReSym.Core.Entities.GameInfo;
using Serilog;
using GReSym.Core.Entities.Feedback;
using Microsoft.Extensions.Logging;

namespace GReSym.Parser.UI;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ---------------- CONFIG ----------------
        var configuration = BuildConfiguration(args);

        // ---------------- LOGGING (SERILOG) ----------------
        ConfigureLogging(configuration);

        try
        {
            Log.Information("Starting GReSym Parser UI");

            var services = new ServiceCollection();

            ConfigureServices(services, configuration);

            var serviceProvider = services.BuildServiceProvider();

            Application.Init();

            var app = new Application(
                "org.GReSym.DesktopParser.GReSym.DesktopParser",
                GLib.ApplicationFlags.None);

            app.Register(GLib.Cancellable.Current);

            var win = new ParserWindow(serviceProvider);

            app.AddWindow(win);
            win.Show();

            Application.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // ---------------- CONFIG ----------------
    private static IConfiguration BuildConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    // ---------------- SERILOG ----------------
    private static void ConfigureLogging(IConfiguration configuration)
    {
        Directory.CreateDirectory("logs");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)

            // FILE → всё
            .WriteTo.File(
                path: "logs/parser-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)

            // CONSOLE → только ошибки
            .WriteTo.Console(
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)

            .CreateLogger();
    }

    // ---------------- DI ----------------
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Подключаем Serilog в ILogger<T>
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // ---------------- DATABASE ----------------
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        if (connectionString.Contains("Server=localhost")
            && connectionString.Contains("Database=GamesRecommend"))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString));
            });
        }
        else
        {
            throw new NotSupportedException(
                $"Unsupported database provider: {connectionString}");
        }

        // ---------------- CONFIG ----------------
        services.AddSingleton(configuration);

        // ---------------- REPOSITORIES ----------------
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ---------------- PARSER ----------------
        services.AddScoped<IParserEngine, ParserEngine>();
        services.AddScoped<IParserStateManager, FileSystemParserStateManager>();

        // ---------------- ETL ----------------
        services.AddScoped<SteamExtractor>();
        services.AddScoped<IDataExtractor<Game>, SteamExtractor>();

        services.AddScoped<GameLoader>();
        services.AddScoped<IDataLoader<Game>, GameLoader>();

        services.AddScoped<SteamReviewExtractor>();
        services.AddScoped<IDataLoader<Review>, ReviewLoader>();
        services.AddScoped<IDataExtractor<Review>>(sp => sp.GetRequiredService<SteamReviewExtractor>());
    }
}