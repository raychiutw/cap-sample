using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PMP.EdgeService.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PmpDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("PMPDB"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(180);
                    /*
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    */
                });
            options.UseUpperSnakeCaseNamingConvention();
        });

        services.AddDbContext<CapDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("PMPDB"));
            options.UseUpperSnakeCaseNamingConvention();
        });

        return services;
    }
}