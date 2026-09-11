namespace Bms.Store.Application.SeedData;

/// <summary>
/// 门店管理系统菜单种子数据定义
/// </summary>
/// <remarks>
/// 【菜单添加规则 - 修改前必读】
///
/// 一、菜单层级与 Type 取值：
///   - Type=0 目录：仅做分组，不对应页面，Component 必须为 null
///   - Type=1 菜单：对应前端页面，Component 必须指向 Vue 组件路径
///   - Type=2 按钮：对应页面内操作权限，不需要 Path 和 Component
///
/// 二、Path / Component 与前端的严格对应关系（不一致会导致菜单点击 404）：
///   - Path：对应前端 router/index.ts 中注册的路由路径（不含前导 /）
///   - Component：对应前端 src/views/ 下的文件路径（不含 /index 后缀）
///   - 示例：Component = "store/product/category/index"
///     → 前端页面文件：src/views/store/product/category/index.vue
///     → 前端路由路径：store/product/category
///
/// 三、Code 命名规范（用冒号分隔层级，与 ParentCode 配合建立父子关系）：
///   - 一级目录：store:product
///   - 二级菜单：store:product:category
///   - 操作按钮：store:product:category:add（页面 Code + camelCase 操作名）
///   - ParentCode 必须与父菜单的 Code 完全一致
///
/// 四、新增菜单后的检查清单：
///   1. 前端 router/index.ts 是否已注册对应路由
///   2. 前端 src/views/ 下是否存在对应页面文件
///   3. Path 和 Component 与前端路由/文件路径是否完全一致
///
/// 五、菜单 Id 的生成与 TestSeedData JSON 的关系（曾踩坑，2026-08）：
///   - 本文件定义的菜单由 StoreMenuRegistrationService 启动时调用 System API 创建，
///     Id 由 System 服务端雪花生成，与 System 服务 TestSeedData 目录下的 JSON 无关；
///     因此不要在本文件写死 Id，也不要修改 TestSeedData 中对应 Store 菜单的 Id。
///   - 测试环境若开启 SeedTestData，System 服务的 Menus.json 会 TRUNCATE 后重新加载全部菜单
///     （含 Store 菜单），其 Id 必须以数据库为准；修改该 JSON 的规则见
///     System 服务 Bms.System.Infrastructure/SeedData/TestSeedData.cs 的类注释。
///
/// 六、本文件不做任何角色授权（RoleMenuAuths），菜单/按钮权限由管理员通过 UI 角色管理手动分配；
///     前端页面通过 userStore.hasPermission(code) 控制按钮显隐，Code 必须与本文件定义一致。
/// </remarks>
public static class StoreMenuSeedData
{
    /// <summary>
    /// 子系统编码
    /// </summary>
    public const string SubsystemCode = "StoreManagement";

    /// <summary>
    /// 子系统名称
    /// </summary>
    public const string SubsystemName = "门店管理";

    /// <summary>
    /// 获取菜单定义列表
    /// </summary>
    /// <remarks>
    /// 菜单结构与 System 服务 TestSeedData/Menus.json 的 store 部分保持一致（目录/页面以该文件为准），
    /// 操作按钮依据前端页面实际写操作梳理。Type=1 页面的"页面查看"按钮（:view）在方法末尾自动生成。
    /// </remarks>
    public static List<MenuDefinition> GetMenus()
    {
        var menus = new List<MenuDefinition>
        {
            // ========== 一级页面：首页 ==========
            new MenuDefinition { Code = "store:home", Name = "首页", Path = "/store/home", Component = "store/home/index", Icon = "HomeFilled", Sort = 1, Type = 1, IsAlwaysShow = true },

            // ========== 一级目录：收银管理 ==========
            new MenuDefinition { Code = "store:pos", Name = "收银管理", Path = "/store/pos", Component = null, Icon = "Money", Sort = 2 },
            new MenuDefinition { Code = "store:pos:quick", Name = "快速开单", Path = "/store/pos/quick", Component = "store/pos/quick/index", Icon = "Lightning", Sort = 1, ParentCode = "store:pos", Type = 1 },
            new MenuDefinition { Code = "store:pos:order", Name = "订单管理", Path = "/store/pos/order", Component = "store/pos/order/index", Icon = "List", Sort = 2, ParentCode = "store:pos", Type = 1 },
            // 订单管理按钮：退款/取消订单（详情为读操作，跟随页面查看权限）
            new MenuDefinition { Code = "store:pos:order:refund", Name = "退款", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:pos:order", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:pos:order:cancel", Name = "取消订单", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:pos:order", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：门店管理 ==========
            new MenuDefinition { Code = "store:store", Name = "门店管理", Path = "/store/store", Component = null, Icon = "Shop", Sort = 3 },
            new MenuDefinition { Code = "store:store:profile", Name = "门店档案", Path = "/store/store/profile", Component = "store/store/profile/index", Icon = "Management", Sort = 1, ParentCode = "store:store", Type = 1 },
            new MenuDefinition { Code = "store:store:setting", Name = "门店设置", Path = "/store/store/setting", Component = "store/store/setting/index", Icon = "Setting", Sort = 2, ParentCode = "store:store", Type = 1 },
            // 门店档案按钮（M8 修复：分配用户替代后端硬编码角色判断，操作权限独立控制）
            new MenuDefinition { Code = "store:store:profile:add", Name = "新增门店", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:store:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:store:profile:edit", Name = "编辑门店", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:store:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:store:profile:delete", Name = "删除门店", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:store:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:store:profile:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:store:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:store:profile:assignUser", Name = "分配用户", Path = null, Component = null, Icon = null, Sort = 5, ParentCode = "store:store:profile", Type = 2, IsVisible = false, IsCache = false },
            // 门店设置按钮：保存设置（租户级跨店权益配置 AllowCrossStoreVerify 开关）
            new MenuDefinition { Code = "store:store:setting:save", Name = "保存设置", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:store:setting", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：商品管理 ==========
            new MenuDefinition { Code = "store:product", Name = "商品管理", Path = "/store/product", Component = null, Icon = "Goods", Sort = 4 },
            new MenuDefinition { Code = "store:product:category", Name = "商品分类", Path = "/store/product/category", Component = "store/product/category/index", Icon = "Collection", Sort = 1, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:master", Name = "商品主档", Path = "/store/product/master", Component = "store/product/master/index", Icon = "CopyDocument", Sort = 2, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:profile", Name = "商品档案", Path = "/store/product/profile", Component = "store/product/profile/index", Icon = "Document", Sort = 3, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:bom", Name = "商品BOM", Path = "/store/product/bom", Component = "store/product/bom/index", Icon = "Connection", Sort = 4, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:supplier", Name = "供应商管理", Path = "/store/product/supplier", Component = "store/product/supplier/index", Icon = "OfficeBuilding", Sort = 5, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:price-log", Name = "价格变动记录", Path = "/store/product/price-log", Component = "store/product/price-log/index", Icon = "Histogram", Sort = 6, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:expiry", Name = "效期查询", Path = "/store/product/expiry", Component = "store/product/expiry/index", Icon = "Timer", Sort = 7, ParentCode = "store:product", Type = 1 },
            new MenuDefinition { Code = "store:product:expiry:sales", Name = "效期销售统计", Path = "/store/product/expiry-sales", Component = "store/product/expiry-sales/index", Icon = "TrendCharts", Sort = 8, ParentCode = "store:product", Type = 1 },
            // 商品分类按钮
            new MenuDefinition { Code = "store:product:category:add", Name = "新增分类", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:product:category", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:category:edit", Name = "编辑分类", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:product:category", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:category:delete", Name = "删除分类", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:product:category", Type = 2, IsVisible = false, IsCache = false },
            // 商品主档按钮（租户级共享，Master 字段全租户生效）
            new MenuDefinition { Code = "store:product:master:add", Name = "新增主档", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:product:master", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:master:edit", Name = "编辑主档", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:product:master", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:master:delete", Name = "删除主档", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:product:master", Type = 2, IsVisible = false, IsCache = false },
            // 商品主档 - 编辑门店档案按钮（统一配置 Store 字段，操作权限独立控制）
            new MenuDefinition { Code = "store:product:master:configStore", Name = "编辑门店档案", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:product:master", Type = 2, IsVisible = false, IsCache = false },
            // 商品档案按钮
            new MenuDefinition { Code = "store:product:profile:add", Name = "新增商品", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:product:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:profile:edit", Name = "编辑商品", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:product:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:profile:delete", Name = "删除商品", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:product:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:profile:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:product:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:profile:manageSupplier", Name = "管理供应商", Path = null, Component = null, Icon = null, Sort = 5, ParentCode = "store:product:profile", Type = 2, IsVisible = false, IsCache = false },
            // 供应商管理按钮（添加/设默认/解除品项关联等子操作跟随管理品项权限）
            new MenuDefinition { Code = "store:product:supplier:add", Name = "新增供应商", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:product:supplier", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:supplier:edit", Name = "编辑供应商", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:product:supplier", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:supplier:delete", Name = "删除供应商", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:product:supplier", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:supplier:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:product:supplier", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:supplier:manageProduct", Name = "管理品项", Path = null, Component = null, Icon = null, Sort = 5, ParentCode = "store:product:supplier", Type = 2, IsVisible = false, IsCache = false },
            // 商品BOM按钮
            new MenuDefinition { Code = "store:product:bom:add", Name = "新增BOM", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:product:bom", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:bom:edit", Name = "编辑BOM", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:product:bom", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:product:bom:delete", Name = "删除BOM", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:product:bom", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：项目卡管理 ==========
            new MenuDefinition { Code = "store:treatment", Name = "项目卡管理", Path = "/store/treatment", Component = null, Icon = "Tickets", Sort = 5 },
            new MenuDefinition { Code = "store:treatment:config", Name = "项目卡配置", Path = "/store/treatment/config", Component = "store/treatment/config/index", Icon = "SetUp", Sort = 1, ParentCode = "store:treatment", Type = 1 },
            new MenuDefinition { Code = "store:treatment:sale", Name = "项目卡销售", Path = "/store/treatment/sale", Component = "store/treatment/sale/index", Icon = "Sell", Sort = 2, ParentCode = "store:treatment", Type = 1 },
            new MenuDefinition { Code = "store:treatment:verify", Name = "项目卡核销", Path = "/store/treatment/verify", Component = "store/treatment/verify/index", Icon = "Check", Sort = 3, ParentCode = "store:treatment", Type = 1 },
            new MenuDefinition { Code = "store:treatment:expire", Name = "到期提醒", Path = "/store/treatment/expire", Component = "store/treatment/expire/index", Icon = "Clock", Sort = 4, ParentCode = "store:treatment", Type = 1 },
            new MenuDefinition { Code = "store:treatment:transfer", Name = "项目卡转卡", Path = "/store/treatment/transfer", Component = "store/treatment/transfer/index", Icon = "Sort", Sort = 5, ParentCode = "store:treatment", Type = 1 },
            // 项目卡配置按钮
            new MenuDefinition { Code = "store:treatment:config:add", Name = "新增项目卡", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:treatment:config", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:treatment:config:edit", Name = "编辑项目卡", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:treatment:config", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:treatment:config:delete", Name = "删除项目卡", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:treatment:config", Type = 2, IsVisible = false, IsCache = false },
            // 项目卡销售按钮
            new MenuDefinition { Code = "store:treatment:sale:refund", Name = "退卡", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:treatment:sale", Type = 2, IsVisible = false, IsCache = false },
            // 项目卡转卡按钮
            new MenuDefinition { Code = "store:treatment:transfer:add", Name = "新增转让", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:treatment:transfer", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：样品赠品 ==========
            new MenuDefinition { Code = "store:sample", Name = "样品赠品", Path = "/store/sample", Component = null, Icon = "Present", Sort = 6 },
            new MenuDefinition { Code = "store:sample:receive", Name = "样品/赠品领用", Path = "/store/sample/receive", Component = "store/sample/receive/index", Icon = "Download", Sort = 2, ParentCode = "store:sample", Type = 1 },
            new MenuDefinition { Code = "store:sample:report", Name = "统计报表", Path = "/store/sample/report", Component = "store/sample/report/index", Icon = "DataLine", Sort = 5, ParentCode = "store:sample", Type = 1 },
            // 样品/赠品领用按钮
            new MenuDefinition { Code = "store:sample:receive:add", Name = "新增领用/派发", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:sample:receive", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：采购库存 ==========
            new MenuDefinition { Code = "store:purchase-inventory", Name = "采购库存", Path = "/store/purchase-inventory", Component = null, Icon = "Box", Sort = 7 },
            new MenuDefinition { Code = "store:purchase-inventory:purchase-order", Name = "采购订单", Path = "/store/product/purchase-order", Component = "store/product/purchase-order/index", Icon = "Document", Sort = 1, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:purchase-return", Name = "采购退货", Path = "/store/product/purchase-return", Component = "store/product/purchase-return/index", Icon = "RefreshLeft", Sort = 2, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:inbound", Name = "入库管理", Path = "/store/product/inbound", Component = "store/product/inbound/index", Icon = "Download", Sort = 3, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:outbound", Name = "出库管理", Path = "/store/product/outbound", Component = "store/product/outbound/index", Icon = "Upload", Sort = 4, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:inventory", Name = "库存管理", Path = "/store/product/inventory", Component = "store/product/inventory/index", Icon = "Box", Sort = 5, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:transfer", Name = "库存调拨", Path = "/store/product/transfer", Component = "store/product/transfer/index", Icon = "Switch", Sort = 6, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:check", Name = "库存盘点", Path = "/store/product/check", Component = "store/product/check/index", Icon = "Check", Sort = 7, ParentCode = "store:purchase-inventory", Type = 1 },
            new MenuDefinition { Code = "store:purchase-inventory:alert", Name = "商品预警", Path = "/store/product/inventory/alert", Component = "store/product/inventory/alert/index", Icon = "WarningFilled", Sort = 8, ParentCode = "store:purchase-inventory", Type = 1 },
            // 库存管理按钮
            new MenuDefinition { Code = "store:purchase-inventory:inventory:export", Name = "导出", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:inventory", Type = 2, IsVisible = false, IsCache = false },
            // 商品预警按钮
            new MenuDefinition { Code = "store:purchase-inventory:alert:scan", Name = "立即扫描", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:alert", Type = 2, IsVisible = false, IsCache = false },
            // 采购订单按钮
            new MenuDefinition { Code = "store:purchase-inventory:purchase-order:add", Name = "新增采购订单", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:purchase-order", Type = 2, IsVisible = false, IsCache = false },
            // 采购退货按钮
            new MenuDefinition { Code = "store:purchase-inventory:purchase-return:add", Name = "发起退货", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:purchase-return", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:purchase-inventory:purchase-return:edit", Name = "编辑退货", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:purchase-inventory:purchase-return", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:purchase-inventory:purchase-return:delete", Name = "删除退货", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:purchase-inventory:purchase-return", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:purchase-inventory:purchase-return:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:purchase-inventory:purchase-return", Type = 2, IsVisible = false, IsCache = false },
            // 出库管理按钮
            new MenuDefinition { Code = "store:purchase-inventory:outbound:add", Name = "新增出库", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:outbound", Type = 2, IsVisible = false, IsCache = false },
            // 库存调拨按钮
            new MenuDefinition { Code = "store:purchase-inventory:transfer:add", Name = "新增调拨", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:transfer", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:purchase-inventory:transfer:execute", Name = "执行调拨", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:purchase-inventory:transfer", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:purchase-inventory:transfer:cancel", Name = "取消调拨", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:purchase-inventory:transfer", Type = 2, IsVisible = false, IsCache = false },
            // 库存盘点按钮
            new MenuDefinition { Code = "store:purchase-inventory:check:add", Name = "新增盘点", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:purchase-inventory:check", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：客户管理 ==========
            new MenuDefinition { Code = "store:customer", Name = "客户管理", Path = "/store/customer", Component = null, Icon = "User", Sort = 8 },
            new MenuDefinition { Code = "store:customer:profile", Name = "客户档案", Path = "/store/customer/profile", Component = "store/customer/profile/index", Icon = "UserFilled", Sort = 1, ParentCode = "store:customer", Type = 1 },
            // 服务档案（整合美容档案/体型数据/反应记录/照片管理，页面内选项卡切换）
            // 名称避开已被 store:customer:profile 占用的"客户档案"
            new MenuDefinition { Code = "store:customer:archive", Name = "服务档案", Path = "/store/customer/archive", Component = "store/customer/archive/index", Icon = "Folder", Sort = 2, ParentCode = "store:customer", Type = 1 },
            new MenuDefinition { Code = "store:customer:level", Name = "客户等级", Path = "/store/customer/level", Component = "store/customer/level/index", Icon = "Star", Sort = 3, ParentCode = "store:customer", Type = 1 },
            new MenuDefinition { Code = "store:customer:tag", Name = "客户标签", Path = "/store/customer/tag", Component = "store/customer/tag/index", Icon = "PriceTag", Sort = 4, ParentCode = "store:customer", Type = 1 },
            new MenuDefinition { Code = "store:customer:points", Name = "积分管理", Path = "/store/customer/points", Component = "store/customer/points/index", Icon = "Coin", Sort = 5, ParentCode = "store:customer", Type = 1 },
            new MenuDefinition { Code = "store:customer:consume", Name = "消费记录", Path = "/store/customer/consume", Component = "store/customer/consume/index", Icon = "List", Sort = 6, ParentCode = "store:customer", Type = 1 },
            new MenuDefinition { Code = "store:customer:care", Name = "客户关怀", Path = "/store/customer/care", Component = "store/customer/care/index", Icon = "Present", Sort = 7, ParentCode = "store:customer", Type = 1 },
            // 客户档案按钮（永久删除为软删数据的高危操作，独立控制）
            new MenuDefinition { Code = "store:customer:profile:add", Name = "新增客户", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:edit", Name = "编辑客户", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:delete", Name = "删除客户", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:permanentDelete", Name = "永久删除", Path = null, Component = null, Icon = null, Sort = 5, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:adjustPoints", Name = "手动调整积分", Path = null, Component = null, Icon = null, Sort = 6, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:profile:recharge", Name = "充值", Path = null, Component = null, Icon = null, Sort = 7, ParentCode = "store:customer:profile", Type = 2, IsVisible = false, IsCache = false },
            // 服务档案 - 4 个选项卡的查看权限：系统权限只有目录/菜单/按钮三级，选项卡级控制依托按钮权限实现
            // 选项卡内写操作按钮均挂页面菜单下，Code 以选项卡名区分归属
            new MenuDefinition { Code = "store:customer:archive:beauty", Name = "美容档案", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:bodyData", Name = "体型数据", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:reaction", Name = "反应记录", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:photo", Name = "照片管理", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:beauty:add", Name = "新建美容档案", Path = null, Component = null, Icon = null, Sort = 5, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:beauty:edit", Name = "编辑美容档案", Path = null, Component = null, Icon = null, Sort = 6, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:beauty:delete", Name = "删除美容档案", Path = null, Component = null, Icon = null, Sort = 7, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:bodyData:add", Name = "新增体型记录", Path = null, Component = null, Icon = null, Sort = 8, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:reaction:add", Name = "新增反应记录", Path = null, Component = null, Icon = null, Sort = 9, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:photo:add", Name = "上传照片", Path = null, Component = null, Icon = null, Sort = 10, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:photo:edit", Name = "编辑照片", Path = null, Component = null, Icon = null, Sort = 11, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:archive:photo:delete", Name = "删除照片", Path = null, Component = null, Icon = null, Sort = 12, ParentCode = "store:customer:archive", Type = 2, IsVisible = false, IsCache = false },
            // 客户等级按钮
            new MenuDefinition { Code = "store:customer:level:add", Name = "新增等级", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:level", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:level:edit", Name = "编辑等级", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:customer:level", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:level:delete", Name = "删除等级", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:customer:level", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:level:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:customer:level", Type = 2, IsVisible = false, IsCache = false },
            // 客户标签按钮
            new MenuDefinition { Code = "store:customer:tag:add", Name = "新增标签", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:tag", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:tag:edit", Name = "编辑标签", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:customer:tag", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:tag:delete", Name = "删除标签", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:customer:tag", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:tag:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:customer:tag", Type = 2, IsVisible = false, IsCache = false },
            // 积分管理按钮（积分规则配置）
            new MenuDefinition { Code = "store:customer:points:saveRule", Name = "保存配置", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:points", Type = 2, IsVisible = false, IsCache = false },
            // 客户关怀按钮
            new MenuDefinition { Code = "store:customer:care:markCared", Name = "标记关怀", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:customer:care", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:customer:care:markThanked", Name = "标记感谢", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:customer:care", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：会员储值 ==========
            new MenuDefinition { Code = "store:storedvalue", Name = "会员储值", Path = "/store/storedvalue", Component = null, Icon = "Wallet", Sort = 9 },
            new MenuDefinition { Code = "store:storedvalue:account", Name = "储值账户", Path = "/store/storedvalue/account", Component = "store/storedvalue/account/index", Icon = "CreditCard", Sort = 1, ParentCode = "store:storedvalue", Type = 1 },
            new MenuDefinition { Code = "store:storedvalue:rule", Name = "储值规则", Path = "/store/storedvalue/rule", Component = "store/storedvalue/rule/index", Icon = "SetUp", Sort = 2, ParentCode = "store:storedvalue", Type = 1 },
            new MenuDefinition { Code = "store:storedvalue:log", Name = "储值流水", Path = "/store/storedvalue/log", Component = "store/storedvalue/log/index", Icon = "Memo", Sort = 3, ParentCode = "store:storedvalue", Type = 1 },
            new MenuDefinition { Code = "store:storedvalue:cash-flow", Name = "现金流水", Path = "/store/storedvalue/cash-flow", Component = "store/storedvalue/cash-flow/index", Icon = "Money", Sort = 4, ParentCode = "store:storedvalue", Type = 1 },
            // 储值账户按钮
            new MenuDefinition { Code = "store:storedvalue:account:recharge", Name = "充值", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:storedvalue:account", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:storedvalue:account:refund", Name = "退款", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:storedvalue:account", Type = 2, IsVisible = false, IsCache = false },
            // 储值规则按钮
            new MenuDefinition { Code = "store:storedvalue:rule:add", Name = "新增规则", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:storedvalue:rule", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:storedvalue:rule:edit", Name = "编辑规则", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:storedvalue:rule", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:storedvalue:rule:delete", Name = "删除规则", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:storedvalue:rule", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:storedvalue:rule:batchDelete", Name = "批量删除", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:storedvalue:rule", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：营销管理 ==========
            new MenuDefinition { Code = "store:marketing", Name = "营销管理", Path = "/store/marketing", Component = null, Icon = "Promotion", Sort = 10 },
            new MenuDefinition { Code = "store:marketing:poster", Name = "宣传图片", Path = "/store/marketing/poster", Component = "store/marketing/poster/index", Icon = "PictureFilled", Sort = 1, ParentCode = "store:marketing", Type = 1 },
            new MenuDefinition { Code = "store:marketing:activity", Name = "活动管理", Path = "/store/marketing/activity", Component = "store/marketing/activity/index", Icon = "Flag", Sort = 2, ParentCode = "store:marketing", Type = 1 },
            // 活动管理按钮
            new MenuDefinition { Code = "store:marketing:activity:add", Name = "新增活动", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:marketing:activity", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:marketing:activity:edit", Name = "编辑活动", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:marketing:activity", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:marketing:activity:delete", Name = "删除活动", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:marketing:activity", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：预约管理 ==========
            new MenuDefinition { Code = "store:appointment", Name = "预约管理", Path = "/store/appointment", Component = null, Icon = "Calendar", Sort = 11 },
            new MenuDefinition { Code = "store:appointment:calendar", Name = "预约日历", Path = "/store/appointment/calendar", Component = "store/appointment/calendar/index", Icon = "Calendar", Sort = 1, ParentCode = "store:appointment", Type = 1 },
            new MenuDefinition { Code = "store:appointment:list", Name = "预约列表", Path = "/store/appointment/list", Component = "store/appointment/list/index", Icon = "List", Sort = 2, ParentCode = "store:appointment", Type = 1 },
            // 预约列表按钮（预约日历新增预约跟随页面查看权限）
            new MenuDefinition { Code = "store:appointment:list:add", Name = "新增预约", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:appointment:list", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:appointment:list:edit", Name = "编辑预约", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:appointment:list", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：服务人员 ==========
            new MenuDefinition { Code = "store:technician", Name = "服务人员", Path = "/store/technician", Component = null, Icon = "Tools", Sort = 12 },
            new MenuDefinition { Code = "store:technician:profile", Name = "商家技师", Path = "/store/technician/profile", Component = "store/technician/profile/index", Icon = "User", Sort = 1, ParentCode = "store:technician", Type = 1 },
            new MenuDefinition { Code = "store:technician:statistic", Name = "技师统计", Path = "/store/technician/statistic", Component = "store/technician/statistic/index", Icon = "DataAnalysis", Sort = 2, ParentCode = "store:technician", Type = 1 },
            new MenuDefinition { Code = "store:technician:skill", Name = "技能分类", Path = "/store/technician/skill", Component = "store/technician/skill/index", Icon = "Grid", Sort = 3, ParentCode = "store:technician", Type = 1 },
            // 商家技师按钮
            new MenuDefinition { Code = "store:technician:profile:add", Name = "新增技师", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:technician:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:technician:profile:edit", Name = "编辑技师", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:technician:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:technician:profile:delete", Name = "删除技师", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:technician:profile", Type = 2, IsVisible = false, IsCache = false },
            // 技能分类按钮
            new MenuDefinition { Code = "store:technician:skill:add", Name = "新增分类", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:technician:skill", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:technician:skill:edit", Name = "编辑分类", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:technician:skill", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:technician:skill:delete", Name = "删除分类", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:technician:skill", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：设备管理 ==========
            new MenuDefinition { Code = "store:equipment", Name = "设备管理", Path = "/store/equipment", Component = null, Icon = "Monitor", Sort = 13 },
            new MenuDefinition { Code = "store:equipment:profile", Name = "设备档案", Path = "/store/equipment/profile", Component = "store/equipment/profile/index", Icon = "OfficeBuilding", Sort = 1, ParentCode = "store:equipment", Type = 1 },
            new MenuDefinition { Code = "store:equipment:maintenance", Name = "设备维护", Path = "/store/equipment/maintenance", Component = "store/equipment/maintenance/index", Icon = "Tools", Sort = 2, ParentCode = "store:equipment", Type = 1 },
            new MenuDefinition { Code = "store:equipment:type", Name = "设备类型", Path = "/store/equipment/type", Component = "store/equipment/type/index", Icon = "Collection", Sort = 3, ParentCode = "store:equipment", Type = 1 },
            // 设备档案按钮
            new MenuDefinition { Code = "store:equipment:profile:add", Name = "新增设备", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:equipment:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:profile:edit", Name = "编辑设备", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:equipment:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:profile:delete", Name = "删除设备", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:equipment:profile", Type = 2, IsVisible = false, IsCache = false },
            // 设备维护按钮
            new MenuDefinition { Code = "store:equipment:maintenance:add", Name = "新增保养记录", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:equipment:maintenance", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:maintenance:edit", Name = "编辑保养记录", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:equipment:maintenance", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:maintenance:delete", Name = "删除保养记录", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:equipment:maintenance", Type = 2, IsVisible = false, IsCache = false },
            // 设备类型按钮
            new MenuDefinition { Code = "store:equipment:type:add", Name = "新增设备类型", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:equipment:type", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:type:edit", Name = "编辑设备类型", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:equipment:type", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:equipment:type:delete", Name = "删除设备类型", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:equipment:type", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：房间管理 ==========
            new MenuDefinition { Code = "store:room", Name = "房间管理", Path = "/store/room", Component = null, Icon = "House", Sort = 14 },
            new MenuDefinition { Code = "store:room:profile", Name = "房间档案", Path = "/store/room/profile", Component = "store/room/profile/index", Icon = "House", Sort = 1, ParentCode = "store:room", Type = 1 },
            // 房间档案按钮
            new MenuDefinition { Code = "store:room:profile:add", Name = "新增房间", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:room:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:room:profile:edit", Name = "编辑房间", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:room:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:room:profile:delete", Name = "删除房间", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:room:profile", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:room:profile:toggleStatus", Name = "启用/禁用", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:room:profile", Type = 2, IsVisible = false, IsCache = false },

            // ========== 一级目录：财务管理 ==========
            new MenuDefinition { Code = "store:finance", Name = "财务管理", Path = "/store/finance", Component = null, Icon = "Money", Sort = 15 },
            new MenuDefinition { Code = "store:finance:daily-settlement", Name = "日结管理", Path = "/store/finance/daily-settlement", Component = "store/finance/daily-settlement/index", Icon = "Calendar", Sort = 1, ParentCode = "store:finance", Type = 1 },
            // 日结管理按钮（日结为财务关键操作，汇总/确认/重算/反日结独立控制）
            new MenuDefinition { Code = "store:finance:daily-settlement:summarize", Name = "汇总日结", Path = null, Component = null, Icon = null, Sort = 1, ParentCode = "store:finance:daily-settlement", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:finance:daily-settlement:confirm", Name = "确认日结", Path = null, Component = null, Icon = null, Sort = 2, ParentCode = "store:finance:daily-settlement", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:finance:daily-settlement:recalculate", Name = "重算", Path = null, Component = null, Icon = null, Sort = 3, ParentCode = "store:finance:daily-settlement", Type = 2, IsVisible = false, IsCache = false },
            new MenuDefinition { Code = "store:finance:daily-settlement:reverse", Name = "反日结", Path = null, Component = null, Icon = null, Sort = 4, ParentCode = "store:finance:daily-settlement", Type = 2, IsVisible = false, IsCache = false },
        };

        // 为所有页面菜单（type=1）自动生成"页面查看"按钮
        // 勾选"页面查看"即可让菜单显示并访问页面，操作权限独立控制
        // 避免"取消所有操作按钮后菜单消失导致页面无法访问"的问题
        var pageViewButtons = menus
            .Where(m => m.Type == 1)
            .Select(pageMenu => new MenuDefinition
            {
                Code = $"{pageMenu.Code}:view",
                Name = "页面查看",
                Path = null,
                Component = null,
                Icon = null,
                Sort = 0,
                Type = 2,
                ParentCode = pageMenu.Code,
                IsVisible = true,
                IsCache = false
            })
            .ToList();
        menus.AddRange(pageViewButtons);

        return menus;
    }
}

/// <summary>
/// 菜单定义
/// </summary>
public class MenuDefinition
{
    /// <summary>
    /// 菜单编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 路由路径
    /// </summary>
    /// <remarks>必须与前端 router/index.ts 中的路由路径一致（不含前导 /），否则菜单点击后 404</remarks>
    public string? Path { get; set; }

    /// <summary>
    /// Vue组件路径
    /// </summary>
    /// <remarks>必须与前端 src/views/ 下的文件路径一致，格式为 "目录/.../index"（不含 .vue 后缀）。目录类型菜单设为 null</remarks>
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 菜单类型：0-目录，1-菜单，2-按钮
    /// </summary>
    public int Type { get; set; } = 0;

    /// <summary>
    /// 父菜单编码
    /// </summary>
    public string? ParentCode { get; set; }

    /// <summary>
    /// 是否始终显示
    /// </summary>
    public bool IsAlwaysShow { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool IsCache { get; set; } = true;
}
