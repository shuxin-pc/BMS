namespace Bms.BuildingBlocks.Core.ServiceDiscovery;

/// <summary>
/// 服务注册接口
/// </summary>
public interface IServiceRegistration
{
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="serviceId">服务ID</param>
    /// <param name="host">服务主机地址</param>
    /// <param name="port">服务端口</param>
    /// <param name="tags">服务标签</param>
    void RegisterService(string serviceName, string serviceId, string host, int port, string[] tags);

    /// <summary>
    /// 注销服务
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    void DeregisterService(string serviceId);
}
