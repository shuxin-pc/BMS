using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 内部消息控制器（供其他服务调用触发消息发送）
/// 通过 InternalServiceAuthMiddleware 校验 X-Internal-Service headers
/// </summary>
[ApiController]
[Route("api/internal/messages")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize] // 内部接口需认证（由 InternalServiceAuthMiddleware 设置身份）
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

    /// <summary>
    /// 批量检查业务键是否已存在消息记录（供后台服务去重判断）
    /// </summary>
    [HttpPost("check-biz-exists")]
    public async Task<ApiResponseDto<BizKeyCheckResultDto>> CheckBizExists([FromBody] BizKeyCheckDto dto)
    {
        return await _messageService.CheckBizExistsAsync(dto);
    }
}
