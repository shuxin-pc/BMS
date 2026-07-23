using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户美容档案管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomerBeautyProfilesController : ControllerBase
{
    private readonly ICustomerBeautyProfileAppService _appService;

    public CustomerBeautyProfilesController(ICustomerBeautyProfileAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取客户美容档案分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>> GetList([FromQuery] CustomerBeautyProfileQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取客户美容档案详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CustomerBeautyProfileDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建客户美容档案
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> Create([FromBody] CustomerBeautyProfileCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新客户美容档案
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CustomerBeautyProfileDto>> Update(long id, [FromBody] CustomerBeautyProfileUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除客户美容档案
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除客户美容档案
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}
