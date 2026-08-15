using LockGo.Application.Interfaces;
using LockGo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LockGo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILockerService, LockerService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
