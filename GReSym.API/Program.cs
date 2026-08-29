using GReSym.API.Extensions;
using GReSym.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------- CONFIG ----------------
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:false, reloadOnChange:true)
    .AddEnvironmentVariables();
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));
// ---------------- LOGGING (SERILOG) ----------------
Directory.CreateDirectory("logs");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        "logs/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------- EXTENSIONS ----------------
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddJwtAuthentication(builder.Configuration);

// ---------------- CONTROLLERS ----------------
builder.Services.AddControllers();

static string MapValidationMessage(string? errorMessage)
{
    Console.WriteLine(errorMessage);
    if (string.IsNullOrWhiteSpace(errorMessage))
        return "Invalid value";

    // JSON conversion errors
    if (errorMessage.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
        return "Invalid format";

    // Required field
    if (errorMessage.Contains("field is required", StringComparison.OrdinalIgnoreCase))
        return "Field is required";

    // String length
    if (errorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase))
        return errorMessage;

    if (errorMessage.Contains("minimum length", StringComparison.OrdinalIgnoreCase))
        return errorMessage;

    // Numeric parsing
    if (errorMessage.Contains("is not valid", StringComparison.OrdinalIgnoreCase))
        return "Invalid value";

    return "Invalid value";
}

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0 && x.Key != "request")
            .Select(x => new
            {
                field = x.Key.Replace("$.", ""),
                message = MapValidationMessage(x.Value!.Errors[0].ErrorMessage)
            })
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add(new
            {
                field = "body",
                message = "Request body is required"
            });
        }

        return new BadRequestObjectResult(new
        {
            message = "Invalid request data",
            errors
        });
    };
});

// Swagger (для тестирования API)
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();

// ---------------- MIDDLEWARE ----------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ---------------- ROUTING ----------------
app.MapControllers();

Log.Information("GReSym API started");

app.Run();