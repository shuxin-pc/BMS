using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 内部消息控制器（供其他服务调用触发消息发送）
/// 通过 InternalServiceAuthMiddleware 认证（X-Internal-Service 头）
/// </summary>
[ApiController]
[Route("api/internal/messages")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class InternalMessagesController : ControllerBase
{
    private readonly IMessageAppService _messageService;

    public InternalMessagesController(IMessageAppService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// 内部触发消息发送（自动触发）
    /// </summary>
    [HttpPost("notify")]
    public async Task<ApiResponseDto<bool>> Notify([FromBody] InternalMessageNotifyDto dto)
    {
        return await _messageService.NotifyAsync(dto);
    }
}
