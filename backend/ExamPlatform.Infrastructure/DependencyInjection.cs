using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Infrastructure.AI;
using ExamPlatform.Infrastructure.Documents;
using ExamPlatform.Infrastructure.Exports;
using ExamPlatform.Infrastructure.Identity;
using ExamPlatform.Infrastructure.Persistence;
using ExamPlatform.Infrastructure.Persistence.Interceptors;
using ExamPlatform.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        // Persistence
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                sql.EnableRetryOnFailure(3);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Identity
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Exports & documents
        services.AddSingleton<IExamPdfExporter, QuestPdfExamExporter>();
        services.AddSingleton<IExamWordExporter, OpenXmlExamWordExporter>();
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

        // AI provider, selected by configuration
        services.Configure<AISettings>(configuration.GetSection(AISettings.SectionName));
        var aiProvider = configuration[$"{AISettings.SectionName}:Provider"] ?? AISettings.GeminiProvider;
        if (aiProvider.Equals(AISettings.MockProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAIQuestionGenerator, MockAIQuestionGenerator>();
        }
        else if (aiProvider.Equals(AISettings.GeminiProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAIQuestionGenerator, GeminiAIQuestionGenerator>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<AISettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });
        }
        else
        {
            throw new InvalidOperationException($"Unknown AI provider '{aiProvider}'. Supported: Gemini, Mock.");
        }

        // Development seed
        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.AddScoped<DevelopmentDataSeeder>();
        services.Configure<BootstrapSettings>(configuration.GetSection(BootstrapSettings.SectionName));
        services.AddScoped<AdminBootstrapper>();

        return services;
    }

    /// <summary>
    /// Optionally applies pending migrations, then runs the development seeder (only acts when Seed:Enabled)
    /// and the first-run administrator bootstrap (only acts when Bootstrap values are configured).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool applyMigrations, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(InitializeDatabaseAsync));

        if (applyMigrations && db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(cancellationToken);
        }

        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().EnsureAdminAsync(cancellationToken);
    }
}
