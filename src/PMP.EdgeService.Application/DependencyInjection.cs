using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PMP.EdgeService.Application.Consumers;

namespace PMP.EdgeService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

        services.AddScoped<ISubscriberService, SubscriberService>();

        return services;
    }
}