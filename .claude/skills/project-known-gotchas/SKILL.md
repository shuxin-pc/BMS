---
name: project-known-gotchas
description: Use when 在本项目（BMS 门店管理系统）排查「按 id 查询记录不存在/404」类异常、处理雪花ID主键（long/int）、编写涉及 id 传递或 EF 批量写入/统计累加/「查询-新建」模式的代码、或排查「重复键违反唯一约束 23505」类异常时，检查是否命中已登记的系统性隐患
---

# 项目已知系统性隐患

> 排查异常先查此处；发现新坑按底部模板登记。

## 隐患清单

### #1 long 主键（雪花ID）前端禁止 Number() 转换

- **现象**：列表能显示，按 id 查详情报「记录不存在/404」；后端日志查询 id 尾数为 000
- **根因**：雪花ID（如 `214633214059221504`）超 JS 安全整数；后端 `LongToStringConverter` 已把 long/int 序列化为**字符串**，前端 `Number(id)` 一转换即丢精度
- **正确做法**：id 全程按字符串传递/比较/拼 URL，禁止 `Number()` / `parseInt()` / `+id`

```ts
// ❌ await getAppointment(Number(id))
// ✅ await getAppointment(String(id))
```

- **影响**：所有雪花ID主键实体的 getById / update / delete / 状态流转
- **案例**（2026-08）：预约列表转单→快速开单报「预约不存在」，`pos/quick/index.vue` 对 appointmentId 做 `Number()` 致精度丢失，改 string 修复
- **衍生坑**：前端禁止对 id 做 `typeof id === 'number'` / `typeof id !== 'number'` 判断——id 运行时是字符串，此类判断会误拦正常数据致功能静默无响应
  - **案例**（2026-08）：快速开单购物车服务项目「编辑服务内容」按钮无效果，`editServiceDetail` 中 `typeof item.id !== 'number'` 提前 return，移除判断后修复；跨行匹配 `item.id === product.id` 两侧均为字符串可正常命中

### #2 EF「查询-新建」模式在循环中看不到内存中已 Add 的实体，同键多行重复插入撞唯一索引

- **现象**：
  - 快速开单结算（POST /api/store/orders）报 `23505 重复键违反唯一约束 "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId"`；同一请求内同一 ProductId 含多行明细时必现
  - 保存采购订单（创建即入库）报 `23505 重复键违反唯一约束 "IX_Inventories_TenantId_StoreId_ProductId"`；采购单明细同一商品分多行（如不同过期日期）时必现
  - 门店间调拨入库报同样的 `IX_Inventories_TenantId_StoreId_ProductId`；调入门店首次接收某商品且调拨明细同商品分多批次多行时必现
- **根因**：EF Core 的 `FirstOrDefaultAsync` 只查**数据库**，看不到同一 DbContext 中已 `Add` 未落库的实体。循环「查-增」模式（查不到就 Add）遇同键多行时，第二行查不到刚 Add 的记录 → 重复 Add → `SaveChanges` 撞唯一索引。触发场景：前端允许同商品多行明细（快速开单同服务选不同技师/时段产生多行；采购单同商品不同过期日期分多行；调拨单同商品不同批次分多行）
- **正确做法**：两种修复方案按业务语义选择：
  1. **先按唯一键 `GroupBy` 聚合合并**再统一查询/累加/新建——适用于统计/汇总写入可合并行的场景（如商品销售统计）
  2. **循环内引入内存字典缓存**已查询/已新建的实体，命中直接累加、不再重复 Add——适用于需要**保留逐行独立语义**的场景（如采购/调拨每行独立批次+独立流水，仅汇总表需合并）
  并发场景（多收银台同时结算同商品）需加事务级顾问锁 `pg_advisory_xact_lock` 串行化，或改用 `ON CONFLICT DO UPDATE` upsert
- **影响**：所有「组合唯一索引 + 循环查询-新建」的写入：统计/汇总表（商品销售统计 `IX_ProductSalesStats_*`、项目卡核销统计等）与**库存汇总表 `IX_Inventories_TenantId_StoreId_ProductId`**（采购入库、调拨入库等）
- **案例**（2026-08）：
  - 快速开单含同服务多行明细结算报 23505，`OrderAppService.RecordProductSalesStatAsync` 先按 ProductId 聚合再查询/累加，并加顾问锁防并发双插；退款侧 `RefundProductSalesStatAsync` 同步聚合防重复扣减
  - 保存采购订单（明细同商品两行不同过期日期）报 23505，`PurchaseOrderAppService.CreateAsync` 库存联动循环引入 `inventoryCache` 内存字典（保留逐行批次/流水语义，汇总表按缓存合并累加）修复
  - 门店间调拨（调入方首次接收 + 同商品多批次多行）同类隐患，`StockTransferAppService` 调拨循环引入 `toInventoryCache` 内存字典预检并修复
  - 已排查安全路径：采购退货（出库扣减、逐条 SaveChanges）、库存盘点（单商品处理）、样品领取（不新建汇总）、订单退款主逻辑（已用字典）、手工创建库存（单条）
- **衍生坑**：顾问锁 key 必须用**确定性哈希**——禁止 `HashCode.Combine`（内部使用随机种子，跨调用/跨进程值不同会使锁形同虚设；曾见 OrderNoGenerator/AppointmentNoGenerator/PurchaseOrderNoGenerator/PurchaseReturnNoGenerator/TreatmentCardSaleNoGenerator 5 个生成器踩此坑，2026-08 已统一修复为确定性哈希）

### #3 FluentValidation 自动验证早于 Controller 方法体，DTO 的 Id 字段必须在 body 传入

- **现象**：提交退款（POST /api/store/orders/{id}/refund）报 `400 {"errors":{"OrderId":["订单ID无效"]}}`；URL 中 id 明明正确且后端 Controller 有 `dto.OrderId = id;` 赋值
- **根因**：项目注册了 `AddFluentValidationAutoValidation()`，自动验证在 **Action 方法体执行之前**（ModelState 阶段）运行。Controller 方法体内 `dto.OrderId = id;` 晚于验证，若前端 body 不传 OrderId，验证时 `dto.OrderId = 0`，触发 `RuleFor(x => x.OrderId).GreaterThan(0)` 失败。凡是「DTO 含 Id 字段 + 验证器校验 Id + Controller 在方法体内补 Id」的模式都会命中
- **正确做法**：涉及 Id 校验的 DTO，前端请求体必须显式携带该 Id 字段（雪花ID为字符串，`LongToStringConverter.Read` 支持字符串→long 反序列化）；或后端移除 DTO.Id 验证、改为在 Action 方法体内先赋值再手动验证，或让验证器不校验由路由提供的 Id
- **影响**：所有「DTO 含 Id + 验证器校验 Id + 方法体内补 Id」的写接口（退款 RefundRequestDto 已命中，Cancel 的 OrderCancelDto 不含 Id 故无此问题）
- **案例**（2026-08）：订单退款弹窗提交报「订单ID无效」400，`api/order/index.ts` 的 `refundOrder` 只在 URL 路径传 orderId、body 未传，后端 `RefundRequestDtoValidator.OrderId > 0` 在自动验证阶段对 0 校验失败；前端 body 补传 `orderId: data.orderId` 修复

### #4 单号唯一索引必须是「租户+门店+单号」组合，生成器必须在事务内加顾问锁

- **现象**：创建库存调拨单（POST /api/store/stocktransfers）报 `23505 重复键违反唯一约束 "IX_StockTransfers_TransferNo"`；同租户多门店同一天各自创建第一张调拨单时**必现**（各门店都生成 `TF{yyyyMMdd}001`），同门店并发创建时概率出现
- **根因**：`StockTransferNoGenerator` 按「同租户+同门店」内查 max+1 生成单号（前缀只含日期、不含门店维度），但 `ConfigureStockTransfer` 的 `entity.HasIndex(e => e.TransferNo).IsUnique()` 是 **TransferNo 全局唯一索引**——单号按门店内递增、索引却全局唯一，多门店同天必然撞车。订单（`UX_Orders_Tenant_Store_OrderNo`）/预约/采购单早已改成「(TenantId, StoreId, 单号) 组合唯一」，唯独调拨漏改；且 `StockTransferNoGenerator` 缺 `BuildLockKey`、`CreateAsync` 未在事务内加 `pg_advisory_xact_lock`，「查 max+1」存在 TOCTOU 竞态
- **正确做法**：
  1. 单号索引一律「单号列普通索引 + (TenantId, StoreId, 单号) 组合唯一」，禁用单列全局唯一（生成器按门店内递增，全局唯一必撞车）
  2. 单号生成器一律提供确定性哈希的 `BuildLockKey`（禁止 `HashCode.Combine`），调用侧在**事务内**先 `SELECT pg_advisory_xact_lock({0})` 再生成单号
- **影响**：所有「按门店内递增生成单号」的实体：索引漏改全局唯一 → 多门店同天创建必现 23505；生成器漏锁 → 同门店并发创建概率撞车。已对齐索引+锁：Order/Appointment/PurchaseOrder/PurchaseReturn/TreatmentCardSale；StockTransfer 于 2026-08 修复
- **案例**（2026-08）：
  - 库存调拨创建报 `IX_StockTransfers_TransferNo` 23505，修复=索引改 `UX_StockTransfers_Tenant_Store_TransferNo` 组合唯一（新增迁移 `AddStockTransferNoStoreUnique`）+ `StockTransferNoGenerator` 加 `BuildLockKey` + `StockTransferAppService.CreateAsync` 在事务内加锁后生成单号
  - 全量排查同类隐患发现两处遗漏（索引均为组合唯一、无问题 A，但生成器/调用侧缺锁）：`ParkedOrderNoGenerator` 无 `BuildLockKey` 且 `ParkedOrderAppService.CreateAsync` 无事务无锁（POS 多收银台并发挂单概率撞 `UX_ParkedOrders_Tenant_Store_ParkNo`）；调拨执行批次号生成（`StockBatchTransferHelper.MergeReceiveBatchesAsync` 经 `BatchNoGenerator` 查 count+1）在 `StockTransferAppService.ExecuteAsync` 事务内但无锁，同租户同日多单并发调往同店（即使不同商品，批次号前缀相同）概率撞 `UX_InventoryBatches_Tenant_Store_BatchNo`。修复=`ParkedOrderNoGenerator`/`BatchNoGenerator` 补 `BuildLockKey` + 对应调用侧事务内 `pg_advisory_xact_lock`
  - 已确认安全：手工创建库存批次（`InventoryBatchAppService`，BatchNo 前端输入非自动生成，撞号由数据库正确拒绝）；采购批次号生成（`PurchaseOrderAppService` 已在 (租户,门店,采购日期) 锁内）

### #5 实体导航属性（Product.Master）未显式 Include + 仅单层空安全 → 详情接口 NRE

- **现象**：库存盘点详情弹窗报 `GET /api/store/inventoryChecks/{id}` 500，后端日志 `NullReferenceException` 于 `InventoryCheckAppService.GetByIdAsync` 的 `product?.Master.Name`；列表/选项接口正常
- **根因**：`_dbContext.Products.FirstOrDefaultAsync()` 未 `.Include(p => p.Master)`，而项目**未启用延迟加载**（无 `UseLazyLoadingProxies`）、`Product.Master` 配置为**可选导航**（`HasOne(...).HasForeignKey(...).OnDelete(Restrict)`，无 `IsRequired`）→ 查出的 Product 实体的 `Master` 恒为 null。`product?.Master.Name` 只对 `product` 空安全，`product` 非空时访问 `.Master.Name` 抛 NRE。列表查询与选项查询中 `p.Master.Name` 在 **LINQ 投影内**会被 EF 翻译成 SQL JOIN，故不受影响
- **正确做法**：取实体后访问导航属性前，查询**必须显式 `.Include(p => p.Master)`**，且访问链用**双层空安全** `product?.Master?.Name`（商品存在但主档软删/缺失时 Master 仍可能为 null）。项目内同类正确范例：`AppointmentAppService.cs`（`.Include(p => p.Master)` + `product?.Master?.Name`）、`PointsExchangeAppService`/`SampleGiftReceiveAppService`
- **影响**：所有「详情接口先查 Product 实体再内存访问 `.Master`」的写接口；全项目仅 `InventoryCheckAppService.GetByIdAsync` 命中（2026-08 修复），其余 `product.Master.Name` 均有 Include 保护或处于投影内
- **案例**（2026-08）：库存盘点详情弹窗必现 500，根因=未 Include Master + 单层空安全；修复=查询补 `.Include(p => p.Master)`、访问改 `product?.Master?.Name/.Code`

## 登记模板

```
### #N <一句话标题>
- **现象**：...
- **根因**：...
- **正确做法**：...
- **影响**：...
- **案例**：时间 + 现象 + 修复
```
