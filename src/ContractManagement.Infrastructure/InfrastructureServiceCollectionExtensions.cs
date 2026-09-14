using ContractManagement.Application.Common.Interfaces;
using ContractManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ContractManagement.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IStorageService, LocalStorageService>();
            return services;
        }
    }
}
