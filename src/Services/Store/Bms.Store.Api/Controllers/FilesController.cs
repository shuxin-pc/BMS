using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.Store.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 文件上传控制器。
/// 只做两件事：数据搬运（从 IFormFile 取流、从 ICurrentUser 取租户/门店）与响应包装。
/// 大小、业务类型、扩展名、魔术字节等全部校验，以及 objectKey 拼装，均在 IFileUploadService 内完成，
/// 本控制器不得出现任何判断逻辑 —— 否则各服务接入时会各自复制一套校验，迟早分化出安全漏洞。
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ICurrentUser _currentUser;

    public FilesController(IFileUploadService fileUploadService, ICurrentUser currentUser)
    {
        _fileUploadService = fileUploadService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 上传单个图片文件，返回需要入库保存的对象键。
    /// 租户与门店标识取自当前登录上下文，不接受前端传入，以此保证 objectKey 前缀无法被伪造
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <param name="bizType">业务类型，需命中服务端配置的白名单</param>
    [HttpPost]
    public async Task<ApiResponseDto<FileUploadResult>> Upload([FromForm] IFormFile file, [FromForm] string bizType)
    {
        await using var stream = file.OpenReadStream();
        var outcome = await _fileUploadService.UploadAsync(new FileUploadRequest
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            BizType = bizType,
            TenantId = _currentUser.TenantId ?? 0,
            StoreId = _currentUser.StoreId
        });

        return outcome.Succeeded
            ? ApiResponseDto<FileUploadResult>.Ok(outcome.Result)
            : ApiResponseDto<FileUploadResult>.Fail(outcome.ErrorMessage!);
    }
}
