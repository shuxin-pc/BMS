# 商品 BOM 页面功能设计

## 背景

商品 BOM 页面（`/store/product/bom`）当前前端为 mock 数据实现，后端 Controller 已有 CRUD 但 DTO 字段与前端期望契约不对齐。本设计将前端 mock 替换为真实后端调用，并对齐前后端契约。

## 现状差异

| 维度 | 前端期望（mock 定义） | 后端现状 |
|------|---------------------|---------|
| BomItem 字段 | 含 serviceProductName / consumableProductName / consumableProductCode / unit 冗余字段 | ServiceBomDto 只有 ID + Quantity + 时间戳 |
| 查询方式 | 按服务项目名称、耗材名称模糊匹配 | ServiceBomQueryDto 只支持按 ID 精确匹配 |
| 选项接口 | getServiceProductOptions()、getConsumableOptions() | 后端无对应接口 |

## 设计方案

### 后端改造

#### 1. ServiceBomDto 增加冗余展示字段

```csharp
+ string ServiceProductName      // 来自 ServiceProduct.Master.Name
+ string ConsumableProductName   // 来自 Product.Master.Name
+ string? ConsumableProductCode  // 来自 Product.Master.Code
+ string? Unit                   // 来自 Product.Master.Unit
```

#### 2. ServiceBomQueryDto 增加名称模糊查询

```csharp
+ string? ServiceProductName
+ string? ConsumableProductName
```

#### 3. ServiceBomAppService 改造

- **GetPagedListAsync**：改用 Join（ServiceBom + ServiceProduct + ProductMaster + Product + ProductMaster）一次查询，支持名称模糊，手动构造 DTO（不用 Adapt，参考 ProductAppService.GetOptionsAsync 的 Join 模式）
- **GetByIdAsync**：同样 Join 填充冗余字段
- **GetServiceProductOptionsAsync()**（新增）：Join ServiceProduct + ProductMaster（过滤 type=2），返回 `{Id, Name}`
- **GetConsumableOptionsAsync()**（新增）：Join Product + ProductMaster（过滤 type=3），返回 `{Id, Name, Code, Unit}`
- 选项方法按当前门店（StoreId）过滤

#### 4. ServiceBomsController 增加端点

- `GET /api/store/serviceboms/service-product-options`
- `GET /api/store/serviceboms/consumable-options`
- 保持与现有方法一致：只标注 `[Authorize]`

#### 5. 新增 DTO 文件（Dtos/ServiceBoms/）

- `ServiceProductOptionDto.cs`：`{Id, Name}`
- `ConsumableOptionDto.cs`：`{Id, Name, Code, Unit}`

### 前端改造

#### 1. api/bom/index.ts 完全重写

- 删除所有 mock 数据
- 使用 `request` 函数调用真实后端（路径 `/serviceboms`）
- getBomList 传 serviceProductName / consumableProductName
- getServiceProductOptions 调 `/serviceboms/service-product-options`
- getConsumableOptions 调 `/serviceboms/consumable-options`

#### 2. api/bom/types.ts 微调

- 保持 BomItem 类型（字段已对齐）
- 复用 shared/storeRequest 的 PagedResponse

#### 3. views/store/product/bom/index.vue

- UI 逻辑、字段映射已对齐 mock 类型，基本不动
- 雪花 ID 精度遵循现有模式（types 用 number，运行时是 string）

## 不改动的部分

- ServiceBom 实体（导航属性已配置）
- StoreDbContext（ServiceBoms 配置已完整）
- ServiceBomValidators（校验规则不变）
- EntityMappingConfig 中 ServiceBom->Dto 配置（保留，AppService 改用手动构造后不再走 Adapt）
- 前端 bom/index.vue 的 UI 结构

## 关键技术决策

1. **DTO 构造方式**：GetPagedListAsync 改用 Join + 手动构造 DTO，不用 Include + Adapt。避免 N+1 查询，与 ProductAppService.GetOptionsAsync 现有模式一致
2. **耗材过滤**：按 `Product.Master.Type == 3` 过滤
3. **门店隔离**：选项接口按 `_currentUser.StoreId` 过滤
4. **选项接口归属**：放在 ServiceBomsController 内聚，不修改现有 ProductsController / ProductMastersController / ServiceProductsController

## 涉及文件清单

### 后端（修改）
- `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomDto.cs`
- `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceBomQueryDto.cs`
- `src/Services/Store/Bms.Store.Application/Services/IServiceBomAppService.cs`
- `src/Services/Store/Bms.Store.Application/Services/ServiceBomAppService.cs`
- `src/Services/Store/Bms.Store.Api/Controllers/ServiceBomsController.cs`

### 后端（新增）
- `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ServiceProductOptionDto.cs`
- `src/Services/Store/Bms.Store.Application/Dtos/ServiceBoms/ConsumableOptionDto.cs`

### 前端（修改）
- `src/Admin/mes-admin/src/api/bom/index.ts`
- `src/Admin/mes-admin/src/api/bom/types.ts`
