namespace Bms.BuildingBlocks.Storage.Options;

/// <summary>
/// 对象存储配置，绑定 appsettings.json 的 ObjectStorage 节
/// </summary>
public class ObjectStorageOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// 服务进程访问对象存储的地址
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 写入预签名 URL、交给浏览器访问的地址，必须是浏览器可达的。
    /// 与 Endpoint 分离是因为服务与浏览器可能不在同一网络位置
    /// （服务部署到服务器后，此处需换成服务器实际域名或 IP）
    /// </summary>
    public string PublicEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥。生产环境应通过环境变量注入，不提交到仓库
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥。生产环境应通过环境变量注入，不提交到仓库
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 桶名称。桶不存在时由启动初始化逻辑自动创建，创建后默认为 private
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 预签名 URL 有效期（分钟）。时效越短，URL 被转发传播的风险窗口越小
    /// </summary>
    public int PresignExpireMinutes { get; set; } = 30;

    /// <summary>
    /// 单文件大小上限（字节）
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// 允许的文件扩展名白名单（含点号）
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>
    /// 允许的业务类型白名单。
    /// bizType 是业务概念，由各接入服务在自己的配置中声明，类库不硬编码，
    /// 新服务接入无需改动类库代码
    /// </summary>
    public string[] AllowedBizTypes { get; set; } = [];
}
