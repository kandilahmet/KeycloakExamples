using Application.Abstractions.Repositories.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Persistence.EFCore.DbContexts;
using OrderService.Infrastructure.Persistence.EFCore.Repositories;

namespace OrderService.Infrastructure.Persistence
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var connectionString = configuration.GetSection("MsSqlConnectionString").Value;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'MsSqlConnectionString' not found."
                );
            }

            services.AddDbContext<MsSqlOrderDbContext>(options =>
            {
                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        );
                        sqlOptions.MigrationsAssembly(
                            typeof(MsSqlOrderDbContext).Assembly.GetName().Name
                        );
                    }
                );
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            });

            RegisterRepositories(services);
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped(typeof(IEFCoreReadRepository<>), typeof(EFCoreReadRepository<>));
            services.AddScoped(typeof(IEFCoreWriteRepository<>), typeof(EFCoreWriteRepository<>));
        }
    }
}
