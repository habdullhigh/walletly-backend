using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace Walletly.Infrastructure.BackgroundJobs;

public static class BackgroundJobsServiceCollectionExtensions
{
    // Hangfire storage is PostgreSQL, so queued emails survive restarts. Do not swap in an in-memory store for any
    // shared or production environment. See README.md, "Background jobs".
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, string? connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        return services;
    }
}
