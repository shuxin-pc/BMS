using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 储值账户管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StoredValueAccountsController : ControllerBase
{
    private readonly IStoredValueAccountAppService _appService;

    public StoredValueAccountsController(IStoredValueAccountAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取储值账户分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>> GetList([FromQuery] StoredValueAccountQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取储值账户详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StoredValueAccountDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建储值账户
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StoredValueAccountDto>> Create([FromBody] StoredValueAccountCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新储值账户
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StoredValueAccountDto>> Update(long id, [FromBody] StoredValueAccountUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除储值账户
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除储值账户
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 储值充值（由操作人员手动录入金额，充值发积分）
    /// </summary>
    [HttpPost("recharge")]
    public async Task<ApiResponseDto<StoredValueAccountDto>> Recharge([FromBody] StoredValueRechargeDto dto)
        => await _appService.RechargeAsync(dto);
}
