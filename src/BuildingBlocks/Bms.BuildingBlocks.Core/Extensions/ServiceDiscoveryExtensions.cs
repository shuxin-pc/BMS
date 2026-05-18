using Consul;
using Bms.BuildingBlocks.Core.ServiceDiscovery;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bms.BuildingBlocks.Core.Extensions;

/// <summary>
/// 服务发现扩展方法
/// </summary>
public static class ServiceDiscoveryExtensions
{
    /// <summary>
    /// 添加Consul服务发现
    /// </summary>
    public static IServiceCollection AddConsulServiceDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        var consulHost = configuration.GetValue<string>("Consul:Host") ?? "localhost";
        var consulPort = configuration.GetValue<int>("Consul:Port", 8500);

        services.AddSingleton<IConsulClient>(sp => new ConsulClient(config =>
        {
            config.Address = new Uri($"http://{consulHost}:{consulPort}");
        }));

        services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>();

        return services;
    }

    /// <summary>
    /// 使用Consul服务注册
    /// </summary>
    public static IApplicationBuilder UseConsulServiceRegistration(this IApplicationBuilder app, IHostApplicationLifetime lifetime, IConfiguration configuration)
    {
        var serviceRegistration = app.ApplicationServices.GetRequiredService<IServiceRegistration>();
        var serviceName = configuration.GetValue<string>("Service:Name") ?? throw new ArgumentNullException("Service:Name is not configured");
        var serviceHost = configuration.GetValue<string>("Service:Host") ?? "localhost";
        var servicePort = configuration.GetValue<int>("Service:Port");
        var serviceId = $"{serviceName}-{Guid.NewGuid()}";

        // 应用启动时注册服务
        lifetime.ApplicationStarted.Register(() =>
        {
            serviceRegistration.RegisterService(serviceName, serviceId, serviceHost, servicePort, new[] { "mes", "microservice" });
        });

        // 应用停止时注销服务
        lifetime.ApplicationStopping.Register(() =>
        {
            serviceRegistration.DeregisterService(serviceId);
        });

        return app;
    }
}
