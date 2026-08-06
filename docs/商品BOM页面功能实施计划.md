# 商品 BOM 页面功能实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将商品 BOM 页面前端 mock 数据替换为真实后端 API 调用，对齐前后端契约

**Architecture:** 后端 ServiceBomAppService 改用 Join 查询填充冗余展示字段并支持名称模糊匹配，新增 2 个选项端点；前端 api/bom/index.ts 重写为 request 调用

**Tech Stack:** .NET 8 + EF Core + Mapster（后端）；Vue 3 + TypeScript + Element Plus（前端）

**设计文档:** `docs/商品BOM页面功能设计.md`

---

## Chunk 1: 后端 DTO 与 Service 改造

### Task 1: ServiceBomDto 增加冗余展示字段

**Files:**
- Modify: `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomDto.cs`

- [ ] **Step 1: 修改 ServiceBomDto**

替换整个文件内容：

```csharp
namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM输出 DTO
/// </summary>
public class ServiceBomDto
{
    public long Id { get; set; }
    public long ServiceProductId { get; set; }
    public long ConsumableProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>服务项目名称（来自 ServiceProduct.Master.Name，冗余展示用）</summary>
    public string ServiceProductName { get; set; } = string.Empty;

    /// <summary>耗材商品名称（来自 Product.Master.Name，冗余展示用）</summary>
    public string ConsumableProductName { get; set; } = string.Empty;

    /// <summary>耗材商品编码（来自 Product.Master.Code，冗余展示用）</summary>
    public string? ConsumableProductCode { get; set; }

    /// <summary>耗材单位（来自 Product.Master.Unit，冗余展示用）</summary>
    public string? Unit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Services/Store/Bms.Store.Application/Bms.Store.Application.csproj`
Expected: Build succeeded, 0 errors

### Task 2: ServiceBomQueryDto 增加名称模糊查询字段

**Files:**
- Modify: `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomQueryDto.cs`

- [ ] **Step 1: 修改 ServiceBomQueryDto**

替换整个文件内容：

```csharp
using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务BOM分页查询参数
/// </summary>
public class ServiceBomQueryDto : PagedRequestDto
{
    public long? ServiceProductId { get; set; }
    public long? ConsumableProductId { get; set; }

    /// <summary>服务项目名称（模糊匹配）</summary>
    public string? ServiceProductName { get; set; }

    /// <summary>耗材商品名称（模糊匹配）</summary>
    public string? ConsumableProductName { get; set; }
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Services/Store/Bms.Store.Application/Bms.Store.Application.csproj`
Expected: Build succeeded, 0 errors

### Task 3: 新增 ServiceProductOptionDto

**Files:**
- Create: `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceProductOptionDto.cs`

- [ ] **Step 1: 创建文件**

```csharp
namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务项目轻量选项（用于BOM下拉选择）
/// </summary>
public class ServiceProductOptionDto
{
    /// <summary>服务项目ID（ServiceProduct.Id）</summary>
    public long Id { get; set; }

    /// <summary>服务项目名称（来自 ProductMaster.Name）</summary>
    public string Name { get; set; } = string.Empty;
}
```

### Task 4: 新增 ConsumableOptionDto

**Files:**
- Create: `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ConsumableOptionDto.cs`

- [ ] **Step 1: 创建文件**

```csharp
namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 耗材商品轻量选项（用于BOM下拉选择）
/// </summary>
public class ConsumableOptionDto
{
    /// <summary>耗材商品ID（Product.Id）</summary>
    public long Id { get; set; }

    /// <summary>耗材商品名称（来自 ProductMaster.Name）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>耗材商品编码（来自 ProductMaster.Code）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>耗材单位（来自 ProductMaster.Unit）</summary>
    public string? Unit { get; set; }
}
```

### Task 5: IServiceBomAppService 增加选项方法签名

**Files:**
- Modify: `src/Services/Store/Bms.Store.Application/Services/IServiceBomAppService.cs`

- [ ] **Step 1: 修改接口**

替换整个文件内容：

```csharp
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ServiceBoms;

namespace Bms.Store.Application.Services;

/// <summary>
/// 服务BOM应用服务接口
/// </summary>
public interface IServiceBomAppService
{
    Task<ApiResponseDto<PagedResponseDto<ServiceBomDto>>> GetPagedListAsync(ServiceBomQueryDto query);
    Task<ApiResponseDto<ServiceBomDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ServiceBomDto>> CreateAsync(ServiceBomCreateDto dto);
    Task<ApiResponseDto<ServiceBomDto>> UpdateAsync(ServiceBomUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取服务项目选项列表（用于BOM下拉选择）
    /// </summary>
    Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptionsAsync();

    /// <summary>
    /// 获取耗材商品选项列表（用于BOM下拉选择，仅 type=3 耗材）
    /// </summary>
    Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptionsAsync();
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Services/Store/Bms.Store.Application/Bms.Store.Application.csproj`
Expected: Build succeeded, 0 errors（ServiceBomAppService 暂未实现新方法，编译会报错，下一步修复）

### Task 6: ServiceBomAppService 改造查询逻辑并实现选项方法

**Files:**
- Modify: `src/Services/Store/Bms.Store.Application/Services/ServiceBomAppService.cs`

- [ ] **Step 1: 修改 GetPagedListAsync 方法**

将现有 `GetPagedListAsync` 方法替换为如下实现（使用 Join 查询填充冗余字段，支持名称模糊匹配）：

```csharp
public async Task<ApiResponseDto<PagedResponseDto<ServiceBomDto>>> GetPagedListAsync(ServiceBomQueryDto query)
{
    if (!_currentUser.TenantId.HasValue)
        return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Fail("无法确定当前租户", 401);

    var tenantId = _currentUser.TenantId.Value;

    // Join ServiceBom -> ServiceProduct -> Master（服务项目名称）
    //         + Product -> Master（耗材名称/编码/单位）
    var queryable = from bom in _dbContext.ServiceBoms
                    where !bom.IsDeleted && bom.TenantId == tenantId
                    join sp in _dbContext.ServiceProducts on bom.ServiceProductId equals sp.Id
                    join spm in _dbContext.ProductMasters on sp.MasterId equals spm.Id
                    join p in _dbContext.Products on bom.ConsumableProductId equals p.Id
                    join pm in _dbContext.ProductMasters on p.MasterId equals pm.Id
                    select new
                    {
                        bom.Id,
                        bom.ServiceProductId,
                        ServiceProductName = spm.Name,
                        bom.ConsumableProductId,
                        ConsumableProductName = pm.Name,
                        ConsumableProductCode = pm.Code,
                        Unit = pm.Unit,
                        bom.Quantity,
                        bom.CreatedTime,
                        bom.UpdatedTime
                    };

    if (query.ServiceProductId.HasValue)
        queryable = queryable.Where(x => x.ServiceProductId == query.ServiceProductId.Value);
    if (query.ConsumableProductId.HasValue)
        queryable = queryable.Where(x => x.ConsumableProductId == query.ConsumableProductId.Value);
    if (!string.IsNullOrWhiteSpace(query.ServiceProductName))
        queryable = queryable.Where(x => x.ServiceProductName.Contains(query.ServiceProductName));
    if (!string.IsNullOrWhiteSpace(query.ConsumableProductName))
        queryable = queryable.Where(x => x.ConsumableProductName.Contains(query.ConsumableProductName));

    var total = await queryable.CountAsync();
    var items = await queryable
        .OrderByDescending(x => x.CreatedTime)
        .Skip((query.PageIndex - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToListAsync();

    var list = items.Select(x => new ServiceBomDto
    {
        Id = x.Id,
        ServiceProductId = x.ServiceProductId,
        ServiceProductName = x.ServiceProductName,
        ConsumableProductId = x.ConsumableProductId,
        ConsumableProductName = x.ConsumableProductName,
        ConsumableProductCode = x.ConsumableProductCode,
        Unit = x.Unit,
        Quantity = x.Quantity,
        CreatedAt = x.CreatedTime,
        UpdatedAt = x.UpdatedTime
    }).ToList();

    var result = new PagedResponseDto<ServiceBomDto>
    {
        List = list,
        Total = total,
        PageIndex = query.PageIndex,
        PageSize = query.PageSize
    };
    return ApiResponseDto<PagedResponseDto<ServiceBomDto>>.Ok(result);
}
```

- [ ] **Step 2: 修改 GetByIdAsync 方法**

将现有 `GetByIdAsync` 方法替换为如下实现（使用 Join 填充冗余字段）：

```csharp
public async Task<ApiResponseDto<ServiceBomDto?>> GetByIdAsync(long id)
{
    if (!_currentUser.TenantId.HasValue)
        return ApiResponseDto<ServiceBomDto?>.Fail("无法确定当前租户", 401);

    var tenantId = _currentUser.TenantId.Value;

    var item = await (from bom in _dbContext.ServiceBoms
                      where !bom.IsDeleted && bom.TenantId == tenantId && bom.Id == id
                      join sp in _dbContext.ServiceProducts on bom.ServiceProductId equals sp.Id
                      join spm in _dbContext.ProductMasters on sp.MasterId equals spm.Id
                      join p in _dbContext.Products on bom.ConsumableProductId equals p.Id
                      join pm in _dbContext.ProductMasters on p.MasterId equals pm.Id
                      select new
                      {
                          bom.Id,
                          bom.ServiceProductId,
                          ServiceProductName = spm.Name,
                          bom.ConsumableProductId,
                          ConsumableProductName = pm.Name,
                          ConsumableProductCode = pm.Code,
                          Unit = pm.Unit,
                          bom.Quantity,
                          bom.CreatedTime,
                          bom.UpdatedTime
                      }).FirstOrDefaultAsync();

    if (item == null)
        return ApiResponseDto<ServiceBomDto?>.Fail("服务BOM不存在", 404);

    var dto = new ServiceBomDto
    {
        Id = item.Id,
        ServiceProductId = item.ServiceProductId,
        ServiceProductName = item.ServiceProductName,
        ConsumableProductId = item.ConsumableProductId,
        ConsumableProductName = item.ConsumableProductName,
        ConsumableProductCode = item.ConsumableProductCode,
        Unit = item.Unit,
        Quantity = item.Quantity,
        CreatedAt = item.CreatedTime,
        UpdatedAt = item.UpdatedTime
    };
    return ApiResponseDto<ServiceBomDto?>.Ok(dto);
}
```

- [ ] **Step 3: 在 ServiceBomAppService 末尾新增 2 个选项方法**

在 `BatchDeleteAsync` 方法之后、类的结束大括号之前，添加：

```csharp
/// <summary>
/// 获取服务项目选项列表（用于BOM下拉选择）
/// ServiceProduct 为租户级共享，按 TenantId 过滤；Join Master 取名称，仅返回 type=2 服务商品
/// </summary>
public async Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptionsAsync()
{
    if (!_currentUser.TenantId.HasValue)
        return ApiResponseDto<List<ServiceProductOptionDto>>.Fail("无法确定当前租户", 401);

    var tenantId = _currentUser.TenantId.Value;

    var options = await (from sp in _dbContext.ServiceProducts
                         where !sp.IsDeleted && sp.TenantId == tenantId
                         join m in _dbContext.ProductMasters on sp.MasterId equals m.Id
                         where m.Type == 2
                         select new ServiceProductOptionDto
                         {
                             Id = sp.Id,
                             Name = m.Name
                         })
                         .OrderBy(x => x.Name)
                         .ToListAsync();

    return ApiResponseDto<List<ServiceProductOptionDto>>.Ok(options);
}

/// <summary>
/// 获取耗材商品选项列表（用于BOM下拉选择）
/// Product 为门店档案，按 TenantId + StoreId 过滤；Join Master 取名称/编码/单位，仅返回 type=3 耗材
/// </summary>
public async Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptionsAsync()
{
    if (!_currentUser.TenantId.HasValue)
        return ApiResponseDto<List<ConsumableOptionDto>>.Fail("无法确定当前租户", 401);

    var tenantId = _currentUser.TenantId.Value;
    var storeId = _currentUser.StoreId ?? 0;

    var options = await (from p in _dbContext.Products
                         where !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId
                         join m in _dbContext.ProductMasters on p.MasterId equals m.Id
                         where m.Type == 3
                         select new ConsumableOptionDto
                         {
                             Id = p.Id,
                             Name = m.Name,
                             Code = m.Code,
                             Unit = m.Unit
                         })
                         .OrderBy(x => x.Name)
                         .ToListAsync();

    return ApiResponseDto<List<ConsumableOptionDto>>.Ok(options);
}
```

- [ ] **Step 4: 验证编译**

Run: `dotnet build src/Services/Store/Bms.Store.Application/Bms.Store.Application.csproj`
Expected: Build succeeded, 0 errors

### Task 7: ServiceBomsController 增加选项端点

**Files:**
- Modify: `src/Services/Store/Bms.Store.Api/Controllers/ServiceBomsController.cs`

- [ ] **Step 1: 在 Controller 末尾（BatchDelete 方法之后）新增 2 个端点**

在 `BatchDelete` 方法之后、类的结束大括号之前，添加：

```csharp
/// <summary>
/// 获取服务项目选项列表（用于BOM下拉选择）
/// </summary>
[HttpGet("service-product-options")]
public async Task<ApiResponseDto<List<ServiceProductOptionDto>>> GetServiceProductOptions()
    => await _appService.GetServiceProductOptionsAsync();

/// <summary>
/// 获取耗材商品选项列表（用于BOM下拉选择，仅 type=3 耗材）
/// </summary>
[HttpGet("consumable-options")]
public async Task<ApiResponseDto<List<ConsumableOptionDto>>> GetConsumableOptions()
    => await _appService.GetConsumableOptionsAsync();
```

注意：需要确认文件顶部已 `using Bms.Store.Application.Dtos.ServiceBoms;`（现有文件已有此 using）。

- [ ] **Step 2: 验证后端整体编译**

Run: `dotnet build src/Services/Store/Bms.Store.Api/Bms.Store.Api.csproj`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit 后端改造**

```bash
git add src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomDto.cs \
        src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomQueryDto.cs \
        src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceProductOptionDto.cs \
        src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ConsumableOptionDto.cs \
        src/Services/Store/Bms.Store.Application/Services/IServiceBomAppService.cs \
        src/Services/Store/Bms.Store.Application/Services/ServiceBomAppService.cs \
        src/Services/Store/Bms.Store.Api/Controllers/ServiceBomsController.cs
git commit -m "feat: ServiceBom 支持名称模糊查询、冗余展示字段和选项接口"
```

---

## Chunk 2: 前端 API 改造

### Task 8: api/bom/types.ts 复用 shared PagedResponse

**Files:**
- Modify: `src/Admin/mes-admin/src/api/bom/types.ts`

- [ ] **Step 1: 修改 types.ts**

替换整个文件内容（删除自有的 PagedResponse 定义，改为从 shared/storeRequest 导入并 re-export）：

```typescript
// ==========================================
// 耗材 BOM 管理类型定义
// 需求 G2.3：耗材与服务项目通过 BOM 关联，服务完成时按 BOM 自动扣减耗材库存
// ==========================================

import type { PagedResponse } from '../shared/storeRequest'

/**
 * BOM 项（服务项目-耗材关联）
 * 对应后端实体 ServiceBom / DTO ServiceBomDto
 */
export interface BomItem {
  /** BOM 记录ID */
  id: number
  /** 服务项目ID（对应 ServiceProduct.Id） */
  serviceProductId: number
  /** 服务项目名称（冗余字段，便于展示） */
  serviceProductName: string
  /** 耗材商品ID（对应 Product.Id） */
  consumableProductId: number
  /** 耗材商品名称（冗余字段，便于展示） */
  consumableProductName: string
  /** 耗材商品编码（冗余字段，便于展示） */
  consumableProductCode?: string
  /** 单次服务消耗数量 */
  quantity: number
  /** 耗材单位（冗余字段，便于展示） */
  unit?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * BOM 查询参数（按服务项目名称/耗材商品名称筛选）
 */
export interface BomQuery {
  /** 服务项目名称（模糊匹配） */
  serviceProductName?: string
  /** 耗材商品名称（模糊匹配） */
  consumableProductName?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建 BOM 项请求
 */
export interface BomCreate {
  /** 服务项目ID */
  serviceProductId: number
  /** 耗材商品ID */
  consumableProductId: number
  /** 单次服务消耗数量 */
  quantity: number
}

/**
 * 更新 BOM 项请求
 */
export interface BomUpdate extends BomCreate {
  /** BOM 记录ID */
  id: number
}

// 复用 shared/storeRequest 的 PagedResponse，re-export 供外部使用
export type { PagedResponse }
```

### Task 9: api/bom/index.ts 重写为真实后端调用

**Files:**
- Modify: `src/Admin/mes-admin/src/api/bom/index.ts`

- [ ] **Step 1: 重写 index.ts**

替换整个文件内容（删除所有 mock 数据，使用 request 函数调用真实后端）：

```typescript
// 耗材 BOM 管理 - API 服务
// 对接后端 ServiceBomsController（路由 api/store/serviceboms）
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate
} from './types'

// 导出类型供外部使用
export type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate,
  PagedResponse
}

// ==================== BOM 管理 ====================

/**
 * 获取 BOM 分页列表（支持按服务项目名称/耗材名称模糊筛选）
 * 对接后端：GET /api/store/serviceboms
 * @param query 查询参数
 * @returns 分页 BOM 列表
 */
export async function getBomList(query?: BomQuery): Promise<PagedResponse<BomItem>> {
  const qs = buildQuery({
    serviceProductName: query?.serviceProductName,
    consumableProductName: query?.consumableProductName,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<BomItem>>(`/serviceboms${qs}`)
}

/**
 * 创建 BOM 项
 * 对接后端：POST /api/store/serviceboms
 * @param data 创建请求
 * @returns 创建后的 BOM 项
 */
export async function createBom(data: BomCreate): Promise<BomItem> {
  return request<BomItem>('/serviceboms', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新 BOM 项
 * 对接后端：PUT /api/store/serviceboms/{id}
 * @param data 更新请求
 * @returns 更新后的 BOM 项
 */
export async function updateBom(data: BomUpdate): Promise<BomItem> {
  return request<BomItem>(`/serviceboms/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除 BOM 项
 * 对接后端：DELETE /api/store/serviceboms/{id}
 * @param id BOM 记录ID
 */
export async function deleteBom(id: number): Promise<void> {
  return request<void>(`/serviceboms/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 下拉选项 ====================

/**
 * 获取服务项目选项列表（用于下拉选择）
 * 对接后端：GET /api/store/serviceboms/service-product-options
 * @returns 服务项目选项数组
 */
export async function getServiceProductOptions(): Promise<{ id: number; name: string }[]> {
  return request<{ id: number; name: string }[]>('/serviceboms/service-product-options')
}

/**
 * 获取耗材商品选项列表（用于下拉选择）
 * 对接后端：GET /api/store/serviceboms/consumable-options
 * @returns 耗材商品选项数组
 */
export async function getConsumableOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  return request<{ id: number; name: string; code: string; unit: string }[]>('/serviceboms/consumable-options')
}
```

- [ ] **Step 2: 验证前端类型检查**

Run: `cd src/Admin/mes-admin && npx vue-tsc --noEmit`
Expected: 0 errors（或与 BOM 无关的既有错误）

- [ ] **Step 3: Commit 前端改造**

```bash
git add src/Admin/mes-admin/src/api/bom/types.ts \
        src/Admin/mes-admin/src/api/bom/index.ts
git commit -m "feat: 商品BOM页面接入真实后端 API，替换 mock 数据"
```

---

## Chunk 3: 验证

### Task 10: 集成验证

**说明:** 此任务为手动验证，无需修改代码

- [ ] **Step 1: 启动后端服务**

确认 Store.Api 能正常启动（需依赖 Identity 和 System 服务鉴权）

- [ ] **Step 2: 启动前端开发服务器**

Run: `cd src/Admin/mes-admin && npm run dev`

- [ ] **Step 3: 访问 BOM 页面验证**

1. 登录系统，选择门店
2. 导航到 `/store/product/bom`
3. 验证：
   - 列表能正常加载（不再显示 mock 数据）
   - 搜索"服务项目名称"和"耗材商品名称"能过滤
   - 新增弹窗中"服务项目"和"耗材商品"下拉能加载选项
   - 选择耗材后单位自动显示
   - 新增/编辑/删除操作正常
   - 分页正常

- [ ] **Step 4: 如有问题，修复后回到 Step 3**

---

## 注意事项

1. **不改动 ServiceBom 实体、StoreDbContext、EntityMappingConfig、Validators** - 这些已配置完整
2. **ServiceBomAppService 的 CreateAsync/UpdateAsync/DeleteAsync/BatchDeleteAsync 保持不变** - 只改 GetPagedListAsync 和 GetByIdAsync
3. **前端 bom/index.vue 不需要修改** - API 函数签名完全兼容，UI 逻辑已对齐 mock 类型
4. **雪花 ID 精度** - 全局 LongToStringConverter 已生效，前端 types 用 number 但运行时接收 string，遵循现有模式
5. **Commit 不加 Co-Authored-By** - 用户全局配置已禁用 attribution
