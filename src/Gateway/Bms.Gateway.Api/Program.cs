using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Bms.BuildingBlocks.MultiTenant.Extensions;
using Bms.BuildingBlocks.MultiTenant.Stores;
using Bms.Gateway.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 添加Ocelot配置
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 添加服务发现
builder.Services.AddOcelot()
    .AddConsul();

// 添加多租户支持（用于获取租户信息进行子系统权限验证）
builder.Services.AddMultiTenant<InMemoryTenantStore>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 使用子系统访问权限验证
app.UseSubsystemAccess();

app.UseMultiTenant();

app.UseAuthorization();

// 使用Ocelot
await app.UseOcelot();

app.MapControllers();

app.Run();
