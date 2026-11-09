using System.Text.Json;
using System.Threading.RateLimiting;
using DauChanDiSan.Api.Data;
using DauChanDiSan.Api.Features.CheckIns.Repositories;
using DauChanDiSan.Api.Features.Discovery;
using DauChanDiSan.Api.Features.Discovery.Repositories;
using DauChanDiSan.Api.Features.Trips;
using DauChanDiSan.Api.Features.Trips.Repositories;
using DauChanDiSan.Api.Infrastructure.Firebase;
using DauChanDiSan.Api.Infrastructure.Gemini;
using DauChanDiSan.Api.Infrastructure.Storage;
using DauChanDiSan.Api.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace DauChanDiSan.Api.Extensions;

public static class BackendExtensions
{
    public static IServiceCollection AddBackend(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase);
        services.AddOpenApi(OpenApiAuth.Configure);
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddHttpContextAccessor();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("register", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10) }));
        });

        var connection = config.GetConnectionString("DauChanDiSan")
            ?? throw new InvalidOperationException("Thiếu cấu hình kết nối DauChanDiSan.");
        services.AddDbContext<DauChanDiSanDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<SqlProcedures>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ContributionRepository>();
        services.AddScoped<CheckInRepository>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<RecommendationService>();
        services.AddScoped<TripAssistantService>();
        services.AddScoped<LocalIntentService>();

        services.AddSingleton<FirebaseIdentity>();
        services.AddSingleton<FirebaseStorage>();
        services.AddSingleton<MediaStorage>();
        services.AddHttpClient<IAssistantProvider, GeminiClient>(client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(45);
        });
        services.AddAuthentication("Firebase")
            .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", _ => { });
        services.AddAuthorizationBuilder()
            .AddPolicy(Roles.Account, policy => policy.RequireAuthenticatedUser().RequireClaim("sql_id"));
        return services;
    }

    public static WebApplication UseBackend(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Dấu Chân Di Sản API v1"));
            app.MapGet("/api/health", async (DauChanDiSanDbContext db, SqlProcedures sql, CancellationToken ct) =>
            {
                if (!await db.Database.CanConnectAsync(ct))
                    return Results.Problem("Không kết nối được SQL Server.", statusCode: 503);
                var rows = await sql.QueryAsync("""
                    IF OBJECT_ID('dbo.PhienBanCSDL','U') IS NOT NULL
                        SELECT PhienBan FROM dbo.PhienBanCSDL WHERE PhienBan='1.4.0';
                    """, ct);
                return rows.Count == 0
                    ? Results.Problem("Hãy chạy toàn bộ DauChanDiSan.sql.", statusCode: 503)
                    : Results.Ok(new { status = "ok" });
            });
        }
        else
        {
            app.UseHttpsRedirection();
        }
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        if (app.Environment.WebRootPath is string root)
        {
            var index = Path.Combine(root, "index.html");
            if (File.Exists(index))
                app.MapFallback(async context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(index, context.RequestAborted);
                });
        }
        return app;
    }
}
