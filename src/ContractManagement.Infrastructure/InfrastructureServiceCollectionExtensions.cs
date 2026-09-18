using System;
using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContractManagement.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration? configuration = null)
        {
            services.AddScoped<IStorageService>(sp =>
            {
                var config = configuration ?? sp.GetRequiredService<IConfiguration>();
                var provider = config["Storage:Provider"] ?? "Database";

                if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
                {
                    return new LocalStorageService(config);
                }

                return new DatabaseStorageService(config, sp.GetService<ILogger<DatabaseStorageService>>());
            });

            return services;
        }
    }
}
