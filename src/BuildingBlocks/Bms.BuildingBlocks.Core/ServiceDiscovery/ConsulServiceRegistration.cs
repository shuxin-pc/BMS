using Consul;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.Core.ServiceDiscovery;

/// <summary>
/// Consul服务注册实现
/// </summary>
public class ConsulServiceRegistration : IServiceRegistration
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceRegistration> _logger;

    public ConsulServiceRegistration(IConsulClient consulClient, ILogger<ConsulServiceRegistration> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    /// <summary>
    /// 注册服务到Consul
    /// </summary>
    public void RegisterService(string serviceName, string serviceId, string host, int port, string[] tags)
    {
        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = host,
            Port = port,
            Tags = tags,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{host}:{port}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            }
        };

        try
        {
            _consulClient.Agent.ServiceRegister(registration).Wait();
            _logger.LogInformation("Service {ServiceName} ({ServiceId}) registered successfully", serviceName, serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register service {ServiceName} ({ServiceId}), service will continue without Consul", serviceName, serviceId);
        }
    }

    /// <summary>
    /// 从Consul注销服务
    /// </summary>
    public void DeregisterService(string serviceId)
    {
        try
        {
            _consulClient.Agent.ServiceDeregister(serviceId).Wait();
            _logger.LogInformation("Service {ServiceId} deregistered successfully", serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service {ServiceId}", serviceId);
        }
    }
}
