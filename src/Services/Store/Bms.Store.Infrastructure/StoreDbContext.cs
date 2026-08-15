using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.MultiTenant.Data;
using Bms.Store.Domain.Entities;

namespace Bms.Store.Infrastructure;

/// <summary>
/// Store 服务数据库上下文
/// </summary>
public class StoreDbContext : TenantDbContext
{
    // 当前租户ID（运行时由 DI 注入 ICurrentUser 提供；设计时迁移为 null，HasQueryFilter 退化为仅过滤软删除）
    private readonly long? _currentTenantId;

    public StoreDbContext(DbContextOptions<StoreDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentTenantId = currentUser?.TenantId;
    }

    // DbSets - 门店管理
    public DbSet<Bms.Store.Domain.Entities.Store> Stores => Set<Bms.Store.Domain.Entities.Store>();
    public DbSet<UserStore> UserStores => Set<UserStore>();
    public DbSet<StoreTenantSetting> StoreTenantSettings => Set<StoreTenantSetting>();
    public DbSet<CrossStoreOperationLog> CrossStoreOperationLogs => Set<CrossStoreOperationLog>();

    // DbSets - 商品管理
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductMaster> ProductMasters => Set<ProductMaster>();
    public DbSet<ServiceProduct> ServiceProducts => Set<ServiceProduct>();
    public DbSet<ServiceProductEquipment> ServiceProductEquipments => Set<ServiceProductEquipment>();
    public DbSet<ServiceProductSkill> ServiceProductSkills => Set<ServiceProductSkill>();
    public DbSet<ServiceBom> ServiceBoms => Set<ServiceBom>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    public DbSet<InventoryAlert> InventoryAlerts => Set<InventoryAlert>();
    public DbSet<PriceChangeLog> PriceChangeLogs => Set<PriceChangeLog>();
    public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

    // DbSets - 客户管理
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerLevel> CustomerLevels => Set<CustomerLevel>();
    public DbSet<CustomerTag> CustomerTags => Set<CustomerTag>();
    public DbSet<CustomerTagLink> CustomerTagLinks => Set<CustomerTagLink>();
    public DbSet<CustomerPointsLog> CustomerPointsLogs => Set<CustomerPointsLog>();
    public DbSet<ConsumeLog> ConsumeLogs => Set<ConsumeLog>();
    public DbSet<CustomerBeautyProfile> CustomerBeautyProfiles => Set<CustomerBeautyProfile>();
    public DbSet<CustomerPreference> CustomerPreferences => Set<CustomerPreference>();
    public DbSet<ServiceReaction> ServiceReactions => Set<ServiceReaction>();
    public DbSet<ServiceComparisonPhoto> ServiceComparisonPhotos => Set<ServiceComparisonPhoto>();
    public DbSet<ServiceComparisonPhotoItem> ServiceComparisonPhotoItems => Set<ServiceComparisonPhotoItem>();
    public DbSet<BodyDataRecord> BodyDataRecords => Set<BodyDataRecord>();
    public DbSet<CustomerDeleteLog> CustomerDeleteLogs => Set<CustomerDeleteLog>();
    public DbSet<CustomerCareLog> CustomerCareLogs => Set<CustomerCareLog>();
    public DbSet<PointsRule> PointsRules => Set<PointsRule>();
    public DbSet<PointsExchange> PointsExchanges => Set<PointsExchange>();

    // DbSets - 会员储值
    public DbSet<StoredValueAccount> StoredValueAccounts => Set<StoredValueAccount>();
    public DbSet<StoredValueRule> StoredValueRules => Set<StoredValueRule>();
    public DbSet<StoredValueLog> StoredValueLogs => Set<StoredValueLog>();

    // DbSets - 经营统计
    public DbSet<DailyStat> DailyStats => Set<DailyStat>();
    public DbSet<MonthlyStat> MonthlyStats => Set<MonthlyStat>();
    public DbSet<ProductSalesStat> ProductSalesStats => Set<ProductSalesStat>();
    public DbSet<DailySettlement> DailySettlements => Set<DailySettlement>();

    // DbSets - 收银管理
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemBatch> OrderItemBatches => Set<OrderItemBatch>();

    // DbSets - 预约管理
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Room> Rooms => Set<Room>();

    // DbSets - 设备管理
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<EquipmentMaintenance> EquipmentMaintenances => Set<EquipmentMaintenance>();
    public DbSet<EquipmentMaintenanceReminder> EquipmentMaintenanceReminders => Set<EquipmentMaintenanceReminder>();

    // DbSets - 疗程卡管理
    public DbSet<TreatmentCard> TreatmentCards => Set<TreatmentCard>();
    public DbSet<TreatmentCardSale> TreatmentCardSales => Set<TreatmentCardSale>();
    public DbSet<TreatmentCardVerify> TreatmentCardVerifies => Set<TreatmentCardVerify>();
    public DbSet<TreatmentCardVerifyItem> TreatmentCardVerifyItems => Set<TreatmentCardVerifyItem>();
    public DbSet<CourseCardItem> CourseCardItems => Set<CourseCardItem>();
    public DbSet<TreatmentCardTransfer> TreatmentCardTransfers => Set<TreatmentCardTransfer>();
    public DbSet<TreatmentCardSaleItem> TreatmentCardSaleItems => Set<TreatmentCardSaleItem>();

    // DbSets - 服务人员
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<TechnicianStatistic> TechnicianStatistics => Set<TechnicianStatistic>();
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<TechnicianSkill> TechnicianSkills => Set<TechnicianSkill>();

    // DbSets - 样品赠品
    public DbSet<SampleGiftReceive> SampleGiftReceives => Set<SampleGiftReceive>();

    // DbSets - 营销管理
    public DbSet<Activity> Activities => Set<Activity>();

    /// <summary>
    /// 是否跳过软删除拦截器（用于永久删除场景）。
    /// SoftDeleteInterceptor 会将 Remove 操作转为 IsDeleted=true 的 Update，
    /// 永久删除需临时开启此开关以绕过拦截，执行真正的物理 DELETE。
    /// </summary>
    public bool SkipSoftDelete { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure multi-tenant schema
        modelBuilder.HasDefaultSchema("bms_store");

        // 全局配置：所有 DateTime 属性默认使用 timestamp without time zone
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("timestamp without time zone");
        }

        ConfigureStore(modelBuilder);
        ConfigureUserStore(modelBuilder);
        ConfigureStoreTenantSetting(modelBuilder);
        ConfigureCrossStoreOperationLog(modelBuilder);
        ConfigureProductMaster(modelBuilder);
        ConfigureProductCategory(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureSupplier(modelBuilder);
        ConfigureProductSupplier(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureInventoryLog(modelBuilder);
        ConfigureInventoryAlert(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureCustomerLevel(modelBuilder);
        ConfigureCustomerTag(modelBuilder);
        ConfigureCustomerTagLink(modelBuilder);
        ConfigureCustomerDeleteLog(modelBuilder);
        ConfigureCustomerCareLog(modelBuilder);
        ConfigureCustomerPointsLog(modelBuilder);
        ConfigureConsumeLog(modelBuilder);
        ConfigureStoredValueAccount(modelBuilder);
        ConfigureStoredValueRule(modelBuilder);
        ConfigureStoredValueLog(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureOrderItemBatch(modelBuilder);
        ConfigureAppointment(modelBuilder);
        ConfigureTreatmentCard(modelBuilder);
        ConfigureTreatmentCardSale(modelBuilder);
        ConfigureTreatmentCardSaleItem(modelBuilder);
        ConfigureTreatmentCardVerify(modelBuilder);
        ConfigureTreatmentCardVerifyItem(modelBuilder);
        ConfigureTechnician(modelBuilder);
        ConfigureTechnicianSkill(modelBuilder);
        ConfigureTechnicianStatistic(modelBuilder);
        ConfigureSampleGiftReceive(modelBuilder);
        ConfigureActivity(modelBuilder);
        ConfigureServiceProduct(modelBuilder);
        ConfigureServiceProductEquipment(modelBuilder);
        ConfigureServiceProductSkill(modelBuilder);
        ConfigurePurchaseReturn(modelBuilder);
        ConfigurePurchaseReturnItem(modelBuilder);
        ConfigureDailyStat(modelBuilder);
        ConfigureMonthlyStat(modelBuilder);
        ConfigureCustomerBeautyProfile(modelBuilder);
        ConfigureCourseCardItem(modelBuilder);
        ConfigurePriceChangeLog(modelBuilder);
        ConfigurePointsExchange(modelBuilder);
        ConfigureInventoryCheck(modelBuilder);
        ConfigureServiceBom(modelBuilder);
        ConfigureRoom(modelBuilder);
        ConfigureEquipmentType(modelBuilder);
        ConfigureEquipment(modelBuilder);
        ConfigureEquipmentMaintenance(modelBuilder);
        ConfigureEquipmentMaintenanceReminder(modelBuilder);
        ConfigurePurchaseOrder(modelBuilder);
        ConfigurePurchaseOrderItem(modelBuilder);
        ConfigurePointsRule(modelBuilder);
        ConfigureInventoryBatch(modelBuilder);
        ConfigureStockTransfer(modelBuilder);
        ConfigureStockTransferItem(modelBuilder);
        ConfigureProductSalesStat(modelBuilder);
        ConfigureCustomerPreference(modelBuilder);
        ConfigureServiceReaction(modelBuilder);
        ConfigureServiceComparisonPhoto(modelBuilder);
        ConfigureServiceComparisonPhotoItem(modelBuilder);
        ConfigureBodyDataRecord(modelBuilder);
        ConfigureSkillCategory(modelBuilder);
        ConfigureDailySettlement(modelBuilder);
        ConfigureTreatmentCardTransfer(modelBuilder);
    }

    private void ConfigureStore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bms.Store.Domain.Entities.Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShortName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BusinessHours).HasMaxLength(100);
            entity.Property(e => e.ManagerName).HasMaxLength(50);
            entity.Property(e => e.LogoUrl).HasColumnType("text");
            entity.Property(e => e.BusinessLicenseUrl).HasColumnType("text");
            entity.Property(e => e.Remark).HasMaxLength(1000);

            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigureUserStore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserStore>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RealName).HasMaxLength(50);
            // 唯一索引：同一用户对同一门店仅允许一条未删除记录，防止重复分配
            // 使用 PostgreSQL 过滤索引语法（项目使用 PostgreSQL）
            entity.HasIndex(e => new { e.UserId, e.StoreId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_UserStores_UserId_StoreId");

            entity.HasIndex(e => new { e.TenantId, e.StoreId }).HasDatabaseName("IX_UserStores_Tenant_Store");
            entity.HasIndex(e => new { e.TenantId, e.UserId }).HasDatabaseName("IX_UserStores_Tenant_User");
        });
    }

    private void ConfigureStoreTenantSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreTenantSetting>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 每租户仅一条配置记录，使用过滤索引保证未删除记录唯一
            entity.HasIndex(e => e.TenantId)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_StoreTenantSettings_Tenant");

            // 角色ID列表用 jsonb 存储，数组语义正确，EF Core+Npgsql 原生支持
            // 列设为 nullable：旧记录迁移后为 null，AppService 读取时用 ?? new List<long>() 兜底
            entity.Property(e => e.BirthdayReminderRoleIds)
                .HasColumnType("jsonb")
                .IsRequired(false);
        });
    }

    private void ConfigureCrossStoreOperationLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CrossStoreOperationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OperatorName).HasMaxLength(50);
            entity.Property(e => e.RequestIp).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.CustomerName).HasMaxLength(50);
            entity.Property(e => e.CustomerPhoneTail).HasMaxLength(10);
            entity.Property(e => e.HomeStoreName).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);

            // 审计日志按 (租户, 操作时间) 索引，便于按时间范围查询审计记录
            entity.HasIndex(e => new { e.TenantId, e.OperationTime })
                .HasDatabaseName("IX_CrossStoreOperationLogs_TenantId_OperationTime");

            // 按操作类型筛选索引，便于按操作类型审计
            entity.HasIndex(e => new { e.TenantId, e.OperationType, e.OperationTime })
                .HasDatabaseName("IX_CrossStoreOperationLogs_TenantId_OperationType_OperationTime");
        });
    }

    private void ConfigureProductMaster(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductMaster>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Specification).HasMaxLength(100);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasColumnType("text");
            entity.Property(e => e.Remark).HasMaxLength(500);

            // Code 在租户内唯一（含软删除过滤，允许删除后重建）
            entity.HasIndex(e => new { e.TenantId, e.Code, e.IsDeleted })
                .IsUnique()
                .HasDatabaseName("UX_ProductMasters_Tenant_Code_Deleted");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ProductMasters_Tenant");
            entity.HasIndex(e => e.CategoryId).HasDatabaseName("IX_ProductMasters_Category");

            // IsSalable 默认 true（样品/赠品由应用层强制 false）
            entity.Property(e => e.IsSalable).HasDefaultValue(true);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureProductCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(50);

            // 分类改租户级共享：Code 在租户内唯一（含软删除过滤）
            entity.HasIndex(e => new { e.TenantId, e.Code, e.IsDeleted })
                .IsUnique()
                .HasDatabaseName("UX_ProductCategories_Tenant_Code_Deleted");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ProductCategories_Tenant");
        });
    }

    private void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.CostPrice).HasPrecision(18, 2);
            entity.Property(e => e.LastPurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.LowStockThreshold).HasPrecision(18, 4);
            entity.Property(e => e.OverstockThreshold).HasPrecision(18, 4);
            entity.Property(e => e.Remark).HasMaxLength(500);

            // 唯一约束：同一门店同一主档只能有一份档案（消除多份档案问题的关键约束）
            // 含软删除过滤，允许删除后重建
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.MasterId, e.IsDeleted })
                .IsUnique()
                .HasDatabaseName("UX_Products_Tenant_Store_Master_Deleted");

            entity.HasIndex(e => e.MasterId).HasDatabaseName("IX_Products_Master");
            entity.HasIndex(e => new { e.TenantId, e.StoreId }).HasDatabaseName("IX_Products_Tenant_Store");

            entity.HasOne(e => e.Master)
                .WithMany()
                .HasForeignKey(e => e.MasterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSupplier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Contact).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BankAccount).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            // 唯一约束：(TenantId, Code, IsDeleted) 唯一，租户内全局唯一（不分公用私用）
            // 含 IsDeleted 是为了让软删除记录不阻止相同 Code 重新创建
            entity.HasIndex(e => new { e.TenantId, e.Code, e.IsDeleted })
                .IsUnique()
                .HasDatabaseName("UX_Suppliers_Tenant_Code_Deleted");
        });
    }

    private void ConfigureActivity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            entity.HasIndex(e => e.StartTime);
        });
    }

    private void ConfigureProductSupplier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductSupplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReferencePrice).HasPrecision(18, 2);
            entity.Property(e => e.LeadTimeDays).HasDefaultValue(0);

            // 唯一约束：(ProductId, SupplierId, TenantId, StoreId) 唯一，按门店隔离避免重复绑定
            entity.HasIndex(e => new { e.ProductId, e.SupplierId, e.TenantId, e.StoreId })
                .IsUnique()
                .HasDatabaseName("UX_ProductSuppliers_Product_Supplier_Tenant_Store");
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.IsDefault });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.ProductId }).IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInventoryLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.BeforeQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AfterQuantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.BatchNo).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            // 活动维度归因查询索引（R5 报表按 ActivityId 聚合）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.ActivityId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supplier 导航属性外键
            entity.HasOne(l => l.Supplier)
                  .WithMany()
                  .HasForeignKey(l => l.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);

            // Activity 导航属性外键（活动软删除时流水保留，ActivityId 置空）
            entity.HasOne(l => l.Activity)
                  .WithMany()
                  .HasForeignKey(l => l.ActivityId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });
    }

    private void ConfigureInventoryAlert(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrentQuantity).HasPrecision(18, 4);
            entity.Property(e => e.AlertValue).HasPrecision(18, 4);
            entity.Property(e => e.ProcessedRemark).HasMaxLength(500);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.AlertType);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.TotalConsume).HasPrecision(18, 2);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.LevelId);
            entity.HasIndex(e => e.AuthorizationStatus);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Level)
                .WithMany()
                .HasForeignKey(e => e.LevelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomerLevel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            // Level 字段：等级值，同租户内唯一（由门店自定义）
            entity.Property(e => e.Level).IsRequired();
            entity.Property(e => e.DiscountRate).HasPrecision(5, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            // 唯一索引：同一租户内同一 Level 仅允许一条未删除记录，防止重复创建
            // 使用 PostgreSQL 过滤索引语法（项目使用 PostgreSQL）
            entity.HasIndex(e => new { e.TenantId, e.Level })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_CustomerLevels_Tenant_Level");
        });
    }

    private void ConfigureCustomerTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            // 唯一过滤索引：同门店内标签名称不重复（仅约束未删除记录）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_CustomerTags_Tenant_Store_Name");
        });
    }

    private void ConfigureCustomerTagLink(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerTagLink>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 同一客户同一标签仅允许一条未删除关联
            entity.HasIndex(e => new { e.CustomerId, e.TagId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_CustomerTagLinks_Customer_Tag");
            entity.HasIndex(e => e.TagId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.CustomerTagLinks)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tag)
                .WithMany()
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomerCareLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerCareLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CareTime).IsRequired();

            // 普通索引：租户+门店隔离查询
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            entity.HasIndex(e => new { e.CustomerId, e.Type, e.CareYear });
            entity.HasIndex(e => new { e.RefOrderId, e.Type });

            // 部分唯一索引：每年每客户仅一条生日关怀（Type=1）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.CustomerId, e.CareYear })
                .IsUnique()
                .HasFilter("\"Type\" = 1")
                .HasDatabaseName("UX_CustomerCareLogs_BirthdayCare");

            // 部分唯一索引：每订单仅一条消费感谢（Type=2）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.RefOrderId })
                .IsUnique()
                .HasFilter("\"Type\" = 2")
                .HasDatabaseName("UX_CustomerCareLogs_ConsumeThank");

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomerPointsLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerPointsLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureConsumeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsumeLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ConsumeTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureStoredValueAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredValueAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.RealBalance).HasPrecision(18, 2);
            entity.Property(e => e.GiftBalance).HasPrecision(18, 2);
            entity.Property(e => e.TotalRecharge).HasPrecision(18, 2);
            entity.Property(e => e.TotalGift).HasPrecision(18, 2);
            entity.Property(e => e.TotalConsume).HasPrecision(18, 2);

            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureStoredValueRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredValueRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.GiftAmount).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            // 充值金额需按租户做重复校验，建索引支撑该查询
            entity.HasIndex(e => new { e.TenantId, e.Amount });
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            // 全局查询过滤器：自动排除已软删除的规则，并按当前租户隔离
            // 设计时（迁移）_currentTenantId 为 null，退化为仅过滤软删除，保证迁移能查到全部数据
            entity.HasQueryFilter(e => !e.IsDeleted && (_currentTenantId == null || e.TenantId == _currentTenantId.Value));
        });
    }

    private void ConfigureStoredValueLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredValueLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.RealAmount).HasPrecision(18, 2);
            entity.Property(e => e.GiftAmount).HasPrecision(18, 2);
            entity.Property(e => e.BeforeBalance).HasPrecision(18, 2);
            entity.Property(e => e.AfterBalance).HasPrecision(18, 2);
            entity.Property(e => e.RealBalanceChange).HasPrecision(18, 2);
            entity.Property(e => e.GiftBalanceChange).HasPrecision(18, 2);
            entity.Property(e => e.BeforeRealBalance).HasPrecision(18, 2);
            entity.Property(e => e.AfterRealBalance).HasPrecision(18, 2);
            entity.Property(e => e.BeforeGiftBalance).HasPrecision(18, 2);
            entity.Property(e => e.AfterGiftBalance).HasPrecision(18, 2);
            entity.Property(e => e.OperatorName).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.DeductRate).HasPrecision(8, 4);
            entity.Property(e => e.RefundReason).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.OrderNo).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BackfillStatus);
            entity.HasIndex(e => e.OrderTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.DiscountRate).HasPrecision(5, 2);
            entity.Property(e => e.DiscountedAmount).HasPrecision(18, 2);
            entity.Property(e => e.TechnicianFee).HasPrecision(18, 2);
            entity.Property(e => e.ConsumableCost).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.TechnicianId);
            // 资源冲突检测索引：技师/房间/设备 同时段占用查询加速
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem 1:N OrderItemBatch（订单明细 -> 批次扣减明细）
            entity.HasMany(e => e.Batches)
                .WithOne(b => b.OrderItem!)
                .HasForeignKey(b => b.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureOrderItemBatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItemBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.CostAmount).HasPrecision(18, 2);
            entity.Property(e => e.RefundedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);

            // 效期销售统计核心索引：按商品 + 效期日期聚合
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.ProductId, e.ExpirationDate });
            // 按订单查询批次明细
            entity.HasIndex(e => e.OrderId);
            // 按订单明细查询批次
            entity.HasIndex(e => e.OrderItemId);
            // 按批次回溯查询（退款场景）
            entity.HasIndex(e => e.BatchId);

            entity.HasOne(e => e.OrderItem)
                .WithMany(o => o.Batches)
                .HasForeignKey(e => e.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // BatchId 可空：原批次可能已删除/合并，仅依靠冗余字段追溯
            entity.HasOne(e => e.Batch)
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureAppointment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AppointmentNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.AppointmentNo).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.RoomId);
            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.AppointmentDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReminderStatus);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Equipment)
                .WithMany()
                .HasForeignKey(e => e.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTreatmentCard(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ServiceItems).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigureTreatmentCardSale(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCardSale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TotalConsumedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CardId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Card)
                .WithMany()
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTreatmentCardSaleItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCardSaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.AllocatedUnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.AllocatedTotalPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.SaleId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.SaleId });
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTreatmentCardVerify(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCardVerify>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VerifyAmount).HasPrecision(18, 2);
            entity.Property(e => e.OperatorName).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CardSaleId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.VerifyTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.CardSale)
                .WithMany()
                .HasForeignKey(e => e.CardSaleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTreatmentCardVerifyItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCardVerifyItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllocatedUnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.SubAmount).HasPrecision(18, 2);

            entity.HasIndex(e => e.VerifyId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.VerifyId });
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Verify)
                .WithMany(v => v.Items)
                .HasForeignKey(e => e.VerifyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureTechnician(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Technician>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AvatarUrl).HasColumnType("text");
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigureTechnicianSkill(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TechnicianSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProficiencyLevel);

            entity.HasIndex(e => new { e.TechnicianId, e.SkillCategoryId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });
    }

    private void ConfigureTechnicianStatistic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TechnicianStatistic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalTechnicianFee).HasPrecision(18, 2);

            entity.HasIndex(e => e.TechnicianId);
            entity.HasIndex(e => e.StatDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.StatDate });

            entity.HasOne(e => e.Technician)
                .WithMany()
                .HasForeignKey(e => e.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSampleGiftReceive(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SampleGiftReceive>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.InventoryBatchId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ReceiveTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.InventoryBatch)
                .WithMany()
                .HasForeignKey(e => e.InventoryBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Activity 导航属性外键（活动软删除时领用记录保留，ActivityId 置空）
            entity.HasOne(e => e.Activity)
                .WithMany()
                .HasForeignKey(e => e.ActivityId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureServiceProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApplicableSkills).HasMaxLength(500);

            // 租户级：MasterId 在租户内唯一（一个 Master 对应一个 ServiceProduct，1:1）
            entity.HasIndex(e => new { e.TenantId, e.MasterId })
                .IsUnique()
                .HasDatabaseName("UX_ServiceProducts_Tenant_Master");
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_ServiceProducts_Tenant");

            entity.HasOne(e => e.Master)
                .WithOne(m => m.ServiceProduct)
                .HasForeignKey<ServiceProduct>(e => e.MasterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureServiceProductEquipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceProductEquipment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ServiceProductId);
            entity.HasIndex(e => e.EquipmentTypeId);
            // 同一服务项目下同一设备类型不重复
            entity.HasIndex(e => new { e.ServiceProductId, e.EquipmentTypeId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.ServiceProduct)
                .WithMany(p => p.RequiredEquipmentTypes)
                .HasForeignKey(e => e.ServiceProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EquipmentType)
                .WithMany()
                .HasForeignKey(e => e.EquipmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureServiceProductSkill(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceProductSkill>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ServiceProductId);
            entity.HasIndex(e => e.SkillCategoryId);
            // 同一门店下同一服务项目同一技能分类不重复
            entity.HasIndex(e => new { e.ServiceProductId, e.StoreId, e.SkillCategoryId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.ServiceProduct)
                .WithMany(p => p.ServiceProductSkills)
                .HasForeignKey(e => e.ServiceProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SkillCategory)
                .WithMany()
                .HasForeignKey(e => e.SkillCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureEquipmentType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Spec).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Code 在租户+门店范围内唯一
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            entity.HasIndex(e => e.IsActive);
        });
    }

    private void ConfigurePurchaseReturn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseReturn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalQuantity).HasPrecision(18, 4);
            entity.Property(e => e.TotalRefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.VoucherImageUrl).HasColumnType("text");
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.ReturnNo);
            // ReturnNo 在"同租户同门店"内唯一（应用层创建/更新时校验，数据库索引兜底）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.ReturnNo })
                .IsUnique()
                .HasDatabaseName("UX_PurchaseReturns_Tenant_Store_ReturnNo");
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ReturnTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // 关联原采购订单（可选）：未传入时为 null，不强制外键约束
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.PurchaseReturn)
                .HasForeignKey(i => i.PurchaseReturnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigurePurchaseReturnItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseReturnItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.BatchNo).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.PurchaseReturnId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureDailyStat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyStat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Revenue).HasPrecision(18, 2);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.GrossProfit).HasPrecision(18, 2);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.CashRefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.StoredValueRecharge).HasPrecision(18, 2);
            entity.Property(e => e.StoredValueConsume).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.StatDate }).IsUnique();

            entity.Property(e => e.TreatmentCardVerifyAmount).HasPrecision(18, 2);

            // 成本维度字段精度（按 InventoryLog.SourceType 拆分）
            entity.Property(e => e.SalesOutboundCost).HasPrecision(18, 2);
            entity.Property(e => e.TreatmentCardOutboundCost).HasPrecision(18, 2);
            entity.Property(e => e.InventoryLossAmount).HasPrecision(18, 2);
            entity.Property(e => e.SampleGiftAmount).HasPrecision(18, 2);
            entity.Property(e => e.TransferOutAmount).HasPrecision(18, 2);
            entity.Property(e => e.TransferInAmount).HasPrecision(18, 2);
            entity.Property(e => e.PurchaseReturnAmount).HasPrecision(18, 2);
        });
    }

    private void ConfigureMonthlyStat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonthlyStat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StatMonth).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Revenue).HasPrecision(18, 2);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.GrossProfit).HasPrecision(18, 2);
            entity.Property(e => e.RefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.StoredValueRecharge).HasPrecision(18, 2);
            entity.Property(e => e.StoredValueConsume).HasPrecision(18, 2);
            entity.Property(e => e.TreatmentCardVerifyAmount).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.StatMonth }).IsUnique();
        });
    }

    private void ConfigureCustomerBeautyProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerBeautyProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SkinType).HasMaxLength(50);
            entity.Property(e => e.Sensitivity).HasMaxLength(200);
            entity.Property(e => e.HairType).HasMaxLength(50);
            entity.Property(e => e.AllergyHistory).HasColumnType("text");
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            // 唯一索引：同租户同门店下每个客户仅允许一份未删除的美容档案
            // 使用 PostgreSQL 过滤索引语法，软删除后允许重建
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.CustomerId })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_CustomerBeautyProfiles_Tenant_Store_Customer");

            // 全局查询过滤器：自动排除已软删除的档案，并按当前租户隔离
            // 设计时（迁移）_currentTenantId 为 null，退化为仅过滤软删除，保证迁移能查到全部数据
            entity.HasQueryFilter(e => !e.IsDeleted && (_currentTenantId == null || e.TenantId == _currentTenantId.Value));

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCourseCardItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseCardItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.AllocatedUnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.AllocatedTotalPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.CourseCardId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.CourseCard)
                .WithMany()
                .HasForeignKey(e => e.CourseCardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePriceChangeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceChangeLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldPrice).HasPrecision(18, 2);
            entity.Property(e => e.NewPrice).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.ChangeTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePointsExchange(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PointsExchange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ExchangeType);
            entity.HasIndex(e => e.ExchangeTime);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureInventoryCheck(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryCheck>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BeforeQuantity).HasPrecision(18, 4);
            entity.Property(e => e.ActualQuantity).HasPrecision(18, 4);
            entity.Property(e => e.DiffQuantity).HasPrecision(18, 4);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.Status).HasDefaultValue(0); // 草稿

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.CheckTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureServiceBom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceBom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);

            entity.HasIndex(e => e.ServiceProductId);
            entity.HasIndex(e => e.ConsumableProductId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.ServiceProduct)
                .WithMany()
                .HasForeignKey(e => e.ServiceProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ConsumableProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureRoom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.RoomType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigureEquipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.EquipmentTypeId);
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.NextMaintenanceDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.EquipmentType)
                .WithMany()
                .HasForeignKey(e => e.EquipmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureEquipmentMaintenance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentMaintenance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.Operator).HasMaxLength(50);
            entity.Property(e => e.Result).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => e.MaintenanceType);
            entity.HasIndex(e => e.MaintenanceDate);
            entity.HasIndex(e => e.NextMaintenanceDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Equipment)
                .WithMany()
                .HasForeignKey(e => e.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureEquipmentMaintenanceReminder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentMaintenanceReminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReminderDate).IsRequired();
            entity.Property(e => e.TargetMaintenanceDate).IsRequired();
            entity.Property(e => e.ReminderType).IsRequired();
            entity.Property(e => e.IsHandled).IsRequired();

            // 支撑每日扫描去重查询与店主端未处理提醒查询
            entity.HasIndex(e => e.EquipmentId);
            entity.HasIndex(e => e.ReminderDate);
            entity.HasIndex(e => e.IsHandled);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            // 同一设备同一提醒日期唯一约束，避免重复生成
            entity.HasIndex(e => new { e.EquipmentId, e.ReminderDate }).IsUnique();

            entity.HasOne(e => e.Equipment)
                .WithMany()
                .HasForeignKey(e => e.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePurchaseOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            // RefundedAmount 默认 0，采购退货冲减时累加；用于报表对账：实际成本 = TotalAmount - RefundedAmount
            entity.Property(e => e.RefundedAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.OrderNo);
            // OrderNo 在"同租户同门店"内唯一（由 PurchaseOrderNoGenerator 应用层保证，数据库索引兜底）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.OrderNo })
                .IsUnique()
                .HasDatabaseName("UX_PurchaseOrders_Tenant_Store_OrderNo");
            entity.HasIndex(e => e.OrderDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PurchaseType);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigurePurchaseOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            entity.Property(e => e.BatchNo).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePointsRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PointsRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PointsRate).HasPrecision(5, 2);
            entity.Property(e => e.DeductRate).HasPrecision(8, 4);
            entity.Property(e => e.MaxDeductAmount).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigureInventoryBatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.BatchNo);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.ProductId });
            // BatchNo 在"同租户同门店"内唯一（由 BatchNoGenerator 应用层保证，数据库索引兜底）
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.BatchNo })
                .IsUnique()
                .HasDatabaseName("UX_InventoryBatches_Tenant_Store_BatchNo");

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureStockTransfer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransferNo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FromStoreCode).HasMaxLength(50);
            entity.Property(e => e.FromStoreName).HasMaxLength(100);
            entity.Property(e => e.ToStoreCode).HasMaxLength(50);
            entity.Property(e => e.ToStoreName).HasMaxLength(100);
            entity.Property(e => e.OperatorName).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.TransferNo).IsUnique();
            entity.HasIndex(e => e.FromStoreId);
            entity.HasIndex(e => e.ToStoreId);
            entity.HasIndex(e => e.TransferDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
        });
    }

    private void ConfigureStockTransferItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockTransferItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.BatchNo).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.StockTransferId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.StockTransfer)
                .WithMany(t => t.Items)
                .HasForeignKey(e => e.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureProductSalesStat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductSalesStat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StatMonth).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SalesAmount).HasPrecision(18, 2);

            entity.HasIndex(e => e.StatDate);
            entity.HasIndex(e => e.StatMonth);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.ProductType);
            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.StatDate, e.ProductId }).IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureCustomerPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TechniquePressure).HasMaxLength(50);
            entity.Property(e => e.Temperature).HasMaxLength(50);
            entity.Property(e => e.MusicPreference).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PreferredTechnicianId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureServiceReaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceItem).HasMaxLength(200);
            entity.Property(e => e.Reaction).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.ReactionDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureServiceComparisonPhoto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceComparisonPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceItem).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.PhotoDate);
            entity.HasIndex(e => e.PhotoType);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureServiceComparisonPhotoItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceComparisonPhotoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            // objectKey 最长约 60 字符，外链留足余量。设上限是为了防止有人重新往里写 base64
            entity.Property(e => e.PhotoSource).HasMaxLength(500);

            entity.HasIndex(e => e.ServiceComparisonPhotoId);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.ServiceComparisonPhoto)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.ServiceComparisonPhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureBodyDataRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BodyDataRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Weight).HasPrecision(5, 1);
            entity.Property(e => e.BodyFat).HasPrecision(5, 1);
            entity.Property(e => e.Bust).HasPrecision(5, 1);
            entity.Property(e => e.Waist).HasPrecision(5, 1);
            entity.Property(e => e.Hip).HasPrecision(5, 1);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.RecordDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCustomerDeleteLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerDeleteLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OriginalPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            // 审计日志按租户+门店隔离 + 删除时间索引，便于按时间范围查询
            entity.HasIndex(e => new { e.TenantId, e.StoreId });
            entity.HasIndex(e => e.DeleteTime);
            entity.HasIndex(e => e.OriginalCustomerId);
        });
    }

    private void ConfigureSkillCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SkillCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);

            // 技能分类改租户级共享：Code 在租户内唯一（含软删除过滤）
            entity.HasIndex(e => new { e.TenantId, e.Code, e.IsDeleted })
                .IsUnique()
                .HasDatabaseName("UX_SkillCategories_Tenant_Code_Deleted");
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_SkillCategories_Tenant");

            entity.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureDailySettlement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailySettlement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.Property(e => e.TotalRefund).HasPrecision(18, 2);
            entity.Property(e => e.TotalStoredValueRecharge).HasPrecision(18, 2);
            entity.Property(e => e.TotalStoredValueConsume).HasPrecision(18, 2);
            entity.Property(e => e.TreatmentCardVerifyAmount).HasPrecision(18, 2);
            entity.Property(e => e.CashRefundAmount).HasPrecision(18, 2);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.ReversedReason).HasMaxLength(500);

            // Source：1=手动汇总，2=系统自动汇总；默认 2 兼容历史自动记录
            entity.Property(e => e.Source).HasDefaultValue(2);

            entity.HasIndex(e => new { e.TenantId, e.StoreId, e.SettlementDate }).IsUnique();

            // 成本维度字段精度（按 InventoryLog.SourceType 拆分）
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalGrossProfit).HasPrecision(18, 2);
            entity.Property(e => e.SalesOutboundCost).HasPrecision(18, 2);
            entity.Property(e => e.TreatmentCardOutboundCost).HasPrecision(18, 2);
            entity.Property(e => e.InventoryLossAmount).HasPrecision(18, 2);
            entity.Property(e => e.SampleGiftAmount).HasPrecision(18, 2);
            entity.Property(e => e.TransferOutAmount).HasPrecision(18, 2);
            entity.Property(e => e.TransferInAmount).HasPrecision(18, 2);
            entity.Property(e => e.PurchaseReturnAmount).HasPrecision(18, 2);
        });
    }

    private void ConfigureTreatmentCardTransfer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreatmentCardTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoreCode).HasMaxLength(50);
            entity.Property(e => e.TransferFee).HasPrecision(18, 2);
            entity.Property(e => e.OperatorName).HasMaxLength(50);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.CardSaleId);
            entity.HasIndex(e => e.FromCustomerId);
            entity.HasIndex(e => e.ToCustomerId);
            entity.HasIndex(e => e.TransferDate);
            entity.HasIndex(e => new { e.TenantId, e.StoreId });

            entity.HasOne(e => e.CardSale)
                .WithMany()
                .HasForeignKey(e => e.CardSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FromCustomer)
                .WithMany()
                .HasForeignKey(e => e.FromCustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToCustomer)
                .WithMany()
                .HasForeignKey(e => e.ToCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
