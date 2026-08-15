using Bms.System.Application.Dtos;
using Bms.System.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bms.System.Api.Filters;

/// <summary>
/// 全局异常过滤器
/// 统一处理业务层抛出的自定义异常，返回规范化错误响应
/// 兜底机制：controller 层已 try-catch 的异常不会到达此处
/// 未处理的异常继续冒泡，由 MultiTenantMiddleware 返回 500
/// 修复 H1/H2 引入的系统性问题：查询接口新增校验后，无 try-catch 的 GET 接口异常冒泡为 500
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case PermissionDeniedException ex:
                // 权限校验失败：业务异常，无需 LogError
                context.Result = new ObjectResult(ApiResponseDto.Fail(ex.Message, 403))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                context.ExceptionHandled = true;
                break;

            case InvalidOperationException ex:
                // 业务操作无效（如"角色不存在"、"系统角色不能删除"）
                _logger.LogWarning(ex, "业务操作无效: {Path}", context.HttpContext.Request.Path);
                context.Result = new ObjectResult(ApiResponseDto.Fail(ex.Message, 400))
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
