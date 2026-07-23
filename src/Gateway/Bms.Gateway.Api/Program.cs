using Microsoft.AspNetCore.HttpOverrides;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Bms.BuildingBlocks.MultiTenant.Extensions;
using Bms.BuildingBlocks.Web.Extensions;
using Bms.Gateway.Api.Middleware;
using Bms.Gateway.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

// 添加Ocelot配置（开发环境叠加 ocelot.Development.json 调高限流，避免联调误伤）
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("ocelot.Development.json", optional: true, reloadOnChange: true);
}

// 读取功能开关（网关作为 http 入口，默认关闭 HttpsRedirection）
var useHttpsRedirection = builder.Configuration.GetValue<bool>("FeatureFlags:UseHttpsRedirection");

// 添加Ocelot（不含服务发现）
builder.Services.AddOcelot();

// 添加多租户支持（通过 HTTP 调用 System.Api 查询租户信息，替代 InMemoryTenantStore）
builder.Services.AddMultiTenant<ApiTenantStore>();

// 注册内存缓存（ApiTenantStore 用于缓存租户信息，减少 HTTP 调用）
builder.Services.AddMemoryCache();

// 注册 System.Api HttpClient（供 ApiTenantStore 调用内部租户查询接口）
builder.Services.AddHttpClient("SystemApi", client =>
{
    var baseUrl = builder.Configuration["SystemApi:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(baseUrl);
});

// 添加JWT认证（与 Identity.Api 使用相同的签名密钥，验证 token 并设置 claims，供 MultiTenant 解析租户）
builder.Services.AddMesAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// 配置CORS（允许前端 dev 端口跨域访问网关）
// ==========================================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// ==========================================
// 配置 ForwardedHeaders（传递真实客户端 IP 给下游服务，用于审计日志 IP 溯源）
// ==========================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 解析 X-Forwarded-For，让 RemoteIpAddress 反映真实客户端 IP（须在 Ocelot 之前）
app.UseForwardedHeaders();

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

// 启用 WebSocket 支持（SignalR 经网关转发必需，必须在 UseOcelot 之前）
app.UseWebSockets();

// JWT认证（必须在 MultiTenant 之前，以便 ClaimTenantResolver 从 claims 解析租户）
app.UseAuthentication();

// 使用子系统访问权限验证
app.UseSubsystemAccess();

app.UseMultiTenant();

app.UseAuthorization();

// 使用Ocelot
await app.UseOcelot();

app.MapControllers();

app.Run();
