using Microsoft.Extensions.DependencyInjection;

namespace LogicFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLogicFitApplication(this IServiceCollection services)
    {
        services.AddScoped<IGymContextAccessor, GymContextAccessor>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IGymScopeService, GymScopeService>();
        return services;
    }
}
