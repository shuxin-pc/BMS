using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bms.Store.Api.Filters;

/// <summary>
/// 门店必填校验 Action Filter。
/// 对 /api/store/ 路径下的请求，若未标记 [AllowWithoutStore] 且 HttpContext.Items["StoreId"] 缺失，
/// 返回 400 + "未绑定门店，请联系管理员"。
/// 与路由守卫、storeRequest 拦截形成前后端纵深防御。
/// </summary>
public class StoreRequiredActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // 仅对 /api/store 路径生效，其他子系统不受影响
        if (!path.StartsWith("/api/store", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // 检查 [AllowWithoutStore] 标记（Controller 或 Action 级别均可）
        var hasAllowAttribute = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is AllowWithoutStoreAttribute);

        if (hasAllowAttribute)
        {
            await next();
            return;
        }

        // 校验 StoreId 是否存在（由 MultiTenantMiddleware 从 X-Store-Id 解析写入）
        var storeId = httpContext.Items["StoreId"];
        if (storeId == null)
        {
            context.Result = new BadRequestObjectResult(new
            {
                code = 400,
                message = "未绑定门店，请联系管理员绑定门店后再访问此功能",
                requestId = httpContext.TraceIdentifier
            });
            return;
        }

        await next();
    }
}
