using Mapster;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.ServiceBoms;
using Bms.Store.Application.Dtos.Rooms;
using Bms.Store.Application.Dtos.Equipments;
using Bms.Store.Application.Dtos.InventoryBatches;
using Bms.Store.Application.Dtos.StockTransfers;
using Bms.Store.Application.Dtos.Statistics;
using StoreEntity = Bms.Store.Domain.Entities.Store;
using ProductEntity = Bms.Store.Domain.Entities.Product;
using ProductCategoryEntity = Bms.Store.Domain.Entities.ProductCategory;
using ServiceBomEntity = Bms.Store.Domain.Entities.ServiceBom;
using RoomEntity = Bms.Store.Domain.Entities.Room;
using EquipmentEntity = Bms.Store.Domain.Entities.Equipment;
using InventoryBatchEntity = Bms.Store.Domain.Entities.InventoryBatch;
using StockTransferEntity = Bms.Store.Domain.Entities.StockTransfer;
using StockTransferItemEntity = Bms.Store.Domain.Entities.StockTransferItem;
using ProductSalesStatEntity = Bms.Store.Domain.Entities.ProductSalesStat;
using SkillCategoryEntity = Bms.Store.Domain.Entities.SkillCategory;
using DailySettlementEntity = Bms.Store.Domain.Entities.DailySettlement;
using TreatmentCardTransferEntity = Bms.Store.Domain.Entities.TreatmentCardTransfer;
using EquipmentMaintenanceEntity = Bms.Store.Domain.Entities.EquipmentMaintenance;
using PurchaseOrderEntity = Bms.Store.Domain.Entities.PurchaseOrder;
using PurchaseOrderItemEntity = Bms.Store.Domain.Entities.PurchaseOrderItem;
using PointsRuleEntity = Bms.Store.Domain.Entities.PointsRule;
using Bms.Store.Application.Dtos.SkillCategories;
using Bms.Store.Application.Dtos.DailySettlements;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;
using Bms.Store.Application.Dtos.PurchaseOrders;
using Bms.Store.Application.Dtos.PointsRules;
using Bms.Store.Application.Dtos.Customers;
using CustomerPreferenceEntity = Bms.Store.Domain.Entities.CustomerPreference;
using ServiceReactionEntity = Bms.Store.Domain.Entities.ServiceReaction;
using ServiceComparisonPhotoEntity = Bms.Store.Domain.Entities.ServiceComparisonPhoto;
using BodyDataRecordEntity = Bms.Store.Domain.Entities.BodyDataRecord;
using Bms.Store.Application.Dtos.Orders;
using Bms.Store.Application.Dtos.Appointments;
using Bms.Store.Application.Dtos.PriceChangeLogs;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Application.Dtos.PurchaseReturns;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Application.Dtos.Points;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Application.Dtos.Technicians;
using Bms.Store.Application.Dtos.SampleGifts;
using Bms.Store.Application.Dtos.Activities;
using CustomerEntity = Bms.Store.Domain.Entities.Customer;
using CustomerLevelEntity = Bms.Store.Domain.Entities.CustomerLevel;
using CustomerTagEntity = Bms.Store.Domain.Entities.CustomerTag;
using CustomerBeautyProfileEntity = Bms.Store.Domain.Entities.CustomerBeautyProfile;
using CustomerPointsLogEntity = Bms.Store.Domain.Entities.CustomerPointsLog;
using ConsumeLogEntity = Bms.Store.Domain.Entities.ConsumeLog;
using OrderEntity = Bms.Store.Domain.Entities.Order;
using OrderItemEntity = Bms.Store.Domain.Entities.OrderItem;
using AppointmentEntity = Bms.Store.Domain.Entities.Appointment;
using PriceChangeLogEntity = Bms.Store.Domain.Entities.PriceChangeLog;
using InventoryEntity = Bms.Store.Domain.Entities.Inventory;
using InventoryAlertEntity = Bms.Store.Domain.Entities.InventoryAlert;
using InventoryCheckEntity = Bms.Store.Domain.Entities.InventoryCheck;
using InventoryLogEntity = Bms.Store.Domain.Entities.InventoryLog;
using PurchaseReturnEntity = Bms.Store.Domain.Entities.PurchaseReturn;
using StoredValueAccountEntity = Bms.Store.Domain.Entities.StoredValueAccount;
using StoredValueLogEntity = Bms.Store.Domain.Entities.StoredValueLog;
using StoredValueRuleEntity = Bms.Store.Domain.Entities.StoredValueRule;
using PointsExchangeEntity = Bms.Store.Domain.Entities.PointsExchange;
using TreatmentCardEntity = Bms.Store.Domain.Entities.TreatmentCard;
using TreatmentCardSaleEntity = Bms.Store.Domain.Entities.TreatmentCardSale;
using TreatmentCardSaleItemEntity = Bms.Store.Domain.Entities.TreatmentCardSaleItem;
using TreatmentCardVerifyEntity = Bms.Store.Domain.Entities.TreatmentCardVerify;
using CourseCardItemEntity = Bms.Store.Domain.Entities.CourseCardItem;
using SupplierEntity = Bms.Store.Domain.Entities.Supplier;
using ProductSupplierEntity = Bms.Store.Domain.Entities.ProductSupplier;
using TechnicianEntity = Bms.Store.Domain.Entities.Technician;
using TechnicianStatisticEntity = Bms.Store.Domain.Entities.TechnicianStatistic;
using DailyStatEntity = Bms.Store.Domain.Entities.DailyStat;
using MonthlyStatEntity = Bms.Store.Domain.Entities.MonthlyStat;
using SampleGiftReceiveEntity = Bms.Store.Domain.Entities.SampleGiftReceive;
using ActivityEntity = Bms.Store.Domain.Entities.Activity;
using ServiceProductEntity = Bms.Store.Domain.Entities.ServiceProduct;

namespace Bms.Store.Application.Mapping;

/// <summary>
/// 对象映射配置
/// </summary>
public static class EntityMappingConfig
{
    public static void Configure()
    {
        // Store -> StoreDto：实体时间字段映射到前端契约字段名
        TypeAdapterConfig<StoreEntity, StoreDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // StoreCreateDto -> Store：忽略审计字段及 ManagerId（前端只录入 ManagerName，ManagerId 由后续关联用户时设置）
        TypeAdapterConfig<StoreCreateDto, StoreEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.ManagerId);

        // Product -> ProductDto：字段名差异映射，子表字段由 AppService 手动填充
        TypeAdapterConfig<ProductEntity, ProductDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Map(d => d.Spec, s => s.Master != null ? s.Master.Specification : null)
            .Map(d => d.CategoryName, s => s.Master != null && s.Master.Category != null ? s.Master.Category.Name : null)
            .Ignore(d => d.Duration)
            .Ignore(d => d.RequiredRoomType)
            .Ignore(d => d.EquipmentTypeIds)
            .Ignore(d => d.EquipmentTypeNames)
            .Ignore(d => d.SkillCategoryIds)
            .Ignore(d => d.SkillCategoryNames);

        // ProductCreateDto -> Product：字段名差异映射，忽略审计字段（子表字段由 AppService 处理，Mapster 自动忽略目标不存在的字段）
        // 注意：Spec/Name/Code 等主档字段已移至 ProductMaster，此映射不再处理这些字段，由 AppService 统一处理
        TypeAdapterConfig<ProductCreateDto, ProductEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.LastPurchasePrice)
            .Ignore(d => d.Master);

        // ProductCategory -> ProductCategoryDto：ParentId(long?) -> long 自动转换（null -> 0），时间字段名映射
        TypeAdapterConfig<ProductCategoryEntity, ProductCategoryDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime);

        // ProductCategoryCreateDto -> ProductCategory：忽略审计字段，ParentId 在 AppService 手动处理
        // ProductCategory 已改为租户级（StoreTenantEntity），不再有 StoreId/StoreCode
        TypeAdapterConfig<ProductCategoryCreateDto, ProductCategoryEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode);

        // ServiceBom -> ServiceBomDto：时间字段映射
        TypeAdapterConfig<ServiceBomEntity, ServiceBomDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ServiceBomCreateDto -> ServiceBom：忽略审计字段
        TypeAdapterConfig<ServiceBomCreateDto, ServiceBomEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.ServiceProduct)
            .Ignore(d => d.Product);

        // Room -> RoomDto：时间字段映射
        TypeAdapterConfig<RoomEntity, RoomDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // RoomCreateDto -> Room：忽略审计字段
        TypeAdapterConfig<RoomCreateDto, RoomEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // Equipment -> EquipmentDto：时间字段映射
        TypeAdapterConfig<EquipmentEntity, EquipmentDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // EquipmentCreateDto -> Equipment：忽略审计字段
        TypeAdapterConfig<EquipmentCreateDto, EquipmentEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // InventoryBatch -> InventoryBatchDto：时间字段映射
        TypeAdapterConfig<InventoryBatchEntity, InventoryBatchDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // InventoryBatchCreateDto -> InventoryBatch：忽略审计字段
        TypeAdapterConfig<InventoryBatchCreateDto, InventoryBatchEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // StockTransfer -> StockTransferDto：时间字段映射
        TypeAdapterConfig<StockTransferEntity, StockTransferDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // StockTransferCreateDto -> StockTransfer：忽略审计字段
        TypeAdapterConfig<StockTransferCreateDto, StockTransferEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // StockTransferItem -> StockTransferItemDto：时间字段映射 + 商品类型从 Master.Type 映射
        TypeAdapterConfig<StockTransferItemEntity, StockTransferItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Map(d => d.Type, s => s.Product.Master.Type);

        // StockTransferItemCreateDto -> StockTransferItem：忽略审计字段
        TypeAdapterConfig<StockTransferItemCreateDto, StockTransferItemEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.StockTransfer)
            .Ignore(d => d.Product);

        // ProductSalesStat -> ProductSalesStatDto：时间字段映射
        TypeAdapterConfig<ProductSalesStatEntity, ProductSalesStatDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ProductSalesStatCreateDto -> ProductSalesStat：忽略审计字段
        TypeAdapterConfig<ProductSalesStatCreateDto, ProductSalesStatEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // SkillCategory -> SkillCategoryDto：时间字段名映射
        TypeAdapterConfig<SkillCategoryEntity, SkillCategoryDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // SkillCategoryCreateDto -> SkillCategory：忽略审计字段
        TypeAdapterConfig<SkillCategoryCreateDto, SkillCategoryEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.Parent);

        // DailySettlement -> DailySettlementDto：时间字段名映射
        TypeAdapterConfig<DailySettlementEntity, DailySettlementDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardTransfer -> TreatmentCardTransferDto：时间字段名映射
        TypeAdapterConfig<TreatmentCardTransferEntity, TreatmentCardTransferDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardTransferCreateDto -> TreatmentCardTransfer：忽略审计字段及导航属性
        TypeAdapterConfig<TreatmentCardTransferCreateDto, TreatmentCardTransferEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.CardSale)
            .Ignore(d => d.FromCustomer)
            .Ignore(d => d.ToCustomer);

        // EquipmentMaintenance -> EquipmentMaintenanceDto：时间字段名映射
        TypeAdapterConfig<EquipmentMaintenanceEntity, EquipmentMaintenanceDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // EquipmentMaintenanceCreateDto -> EquipmentMaintenance：忽略审计字段及导航属性
        TypeAdapterConfig<EquipmentMaintenanceCreateDto, EquipmentMaintenanceEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Equipment);

        // PurchaseOrder -> PurchaseOrderDto：时间字段名映射
        TypeAdapterConfig<PurchaseOrderEntity, PurchaseOrderDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Map(d => d.Items, s => s.OrderItems);

        // PurchaseOrderCreateDto -> PurchaseOrder：忽略审计字段及导航属性
        TypeAdapterConfig<PurchaseOrderCreateDto, PurchaseOrderEntity>
            .NewConfig()
            .Map(d => d.OrderItems, s => s.Items)
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // PurchaseOrderItem -> PurchaseOrderItemDto：时间字段名映射
        TypeAdapterConfig<PurchaseOrderItemEntity, PurchaseOrderItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // PurchaseOrderItemCreateDto -> PurchaseOrderItem：忽略审计字段及导航属性
        TypeAdapterConfig<PurchaseOrderItemCreateDto, PurchaseOrderItemEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.PurchaseOrder)
            .Ignore(d => d.Product);

        // PointsRule -> PointsRuleDto：时间字段名映射
        TypeAdapterConfig<PointsRuleEntity, PointsRuleDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // PointsRuleCreateDto -> PointsRule：忽略审计字段
        TypeAdapterConfig<PointsRuleCreateDto, PointsRuleEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // CustomerPreference -> CustomerPreferenceDto：时间字段名映射
        TypeAdapterConfig<CustomerPreferenceEntity, CustomerPreferenceDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CustomerPreferenceCreateDto -> CustomerPreference：忽略审计字段及导航属性
        TypeAdapterConfig<CustomerPreferenceCreateDto, CustomerPreferenceEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // ServiceReaction -> ServiceReactionDto：时间字段名映射
        TypeAdapterConfig<ServiceReactionEntity, ServiceReactionDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ServiceReactionCreateDto -> ServiceReaction：忽略审计字段及导航属性
        TypeAdapterConfig<ServiceReactionCreateDto, ServiceReactionEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // ServiceComparisonPhoto -> ServiceComparisonPhotoDto：时间字段名映射
        TypeAdapterConfig<ServiceComparisonPhotoEntity, ServiceComparisonPhotoDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ServiceComparisonPhotoCreateDto -> ServiceComparisonPhoto：忽略审计字段及导航属性
        // Items 为提交用结构（含保留标记 Id），需由 AppService 按新增/保留区分处理，不能直接映射
        TypeAdapterConfig<ServiceComparisonPhotoCreateDto, ServiceComparisonPhotoEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer)
            .Ignore(d => d.Items);

        // BodyDataRecord -> BodyDataRecordDto：时间字段名映射
        // CustomerName/Phone 不在实体上，由 AppService 手动填充，此处显式忽略
        TypeAdapterConfig<BodyDataRecordEntity, BodyDataRecordDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Ignore(d => d.CustomerName)
            .Ignore(d => d.CustomerPhone);

        // BodyDataRecordCreateDto -> BodyDataRecord：忽略审计字段及导航属性
        TypeAdapterConfig<BodyDataRecordCreateDto, BodyDataRecordEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // Customer -> CustomerDto：时间字段名映射
        TypeAdapterConfig<CustomerEntity, CustomerDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CustomerCreateDto -> Customer：忽略审计字段及导航属性
        TypeAdapterConfig<CustomerCreateDto, CustomerEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Level)
            .Ignore(d => d.CustomerTagLinks);

        // CustomerLevel -> CustomerLevelDto：时间字段名映射
        TypeAdapterConfig<CustomerLevelEntity, CustomerLevelDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CustomerLevelCreateDto -> CustomerLevel：忽略审计字段
        TypeAdapterConfig<CustomerLevelCreateDto, CustomerLevelEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // CustomerTag -> CustomerTagDto：时间字段名映射
        TypeAdapterConfig<CustomerTagEntity, CustomerTagDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CustomerTagCreateDto -> CustomerTag：忽略审计字段
        TypeAdapterConfig<CustomerTagCreateDto, CustomerTagEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // CustomerBeautyProfile -> CustomerBeautyProfileDto：时间字段名映射
        TypeAdapterConfig<CustomerBeautyProfileEntity, CustomerBeautyProfileDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CustomerBeautyProfileCreateDto -> CustomerBeautyProfile：忽略审计字段及导航属性
        TypeAdapterConfig<CustomerBeautyProfileCreateDto, CustomerBeautyProfileEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // CustomerPointsLog -> CustomerPointsLogDto：时间字段名映射
        // CustomerName/Phone/OrderNo 不在实体上，由 AppService 手动填充，此处显式忽略
        TypeAdapterConfig<CustomerPointsLogEntity, CustomerPointsLogDto>
            .NewConfig()
            .Map(d => d.ChangeTime, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Ignore(d => d.CustomerName)
            .Ignore(d => d.Phone)
            .Ignore(d => d.OrderNo);

        // CustomerPointsLogCreateDto -> CustomerPointsLog：忽略审计字段及导航属性
        TypeAdapterConfig<CustomerPointsLogCreateDto, CustomerPointsLogEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // ConsumeLog -> ConsumeLogDto：时间字段名映射
        TypeAdapterConfig<ConsumeLogEntity, ConsumeLogDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ConsumeLogCreateDto -> ConsumeLog：忽略审计字段及导航属性
        TypeAdapterConfig<ConsumeLogCreateDto, ConsumeLogEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // Order -> OrderDto：时间字段名映射
        TypeAdapterConfig<OrderEntity, OrderDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // OrderCreateDto -> Order：忽略审计字段及导航属性
        TypeAdapterConfig<OrderCreateDto, OrderEntity>
            .NewConfig()
            .Map(d => d.OrderItems, s => s.Items)
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // OrderItem -> OrderItemDto：时间字段名映射
        TypeAdapterConfig<OrderItemEntity, OrderItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // OrderItemBatch -> OrderItemBatchDto：时间字段名映射
        TypeAdapterConfig<Bms.Store.Domain.Entities.OrderItemBatch, OrderItemBatchDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime);

        // OrderItemCreateDto -> OrderItem：忽略审计字段及导航属性
        TypeAdapterConfig<OrderItemCreateDto, OrderItemEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Order)
            .Ignore(d => d.Product)
            .Ignore(d => d.Batches);

        // Appointment -> AppointmentDto：时间字段名映射，ProductName 由 AppService 手动填充
        TypeAdapterConfig<AppointmentEntity, AppointmentDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Ignore(d => d.ProductName);

        // AppointmentCreateDto -> Appointment：忽略审计字段及导航属性
        // EndTime 由 AppService 根据 ServiceProduct.Duration 权威计算，不从 DTO 映射
        TypeAdapterConfig<AppointmentCreateDto, AppointmentEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer)
            .Ignore(d => d.Equipment)
            .Ignore(d => d.Product)
            .Ignore(d => d.EndTime);
        TypeAdapterConfig<PriceChangeLogEntity, PriceChangeLogDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // Inventory -> InventoryDto：时间字段名映射
        TypeAdapterConfig<InventoryEntity, InventoryDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // InventoryCreateDto -> Inventory：忽略审计字段及导航属性
        TypeAdapterConfig<InventoryCreateDto, InventoryEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // InventoryAlert -> InventoryAlertDto：时间字段名映射
        TypeAdapterConfig<InventoryAlertEntity, InventoryAlertDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // InventoryAlertCreateDto -> InventoryAlert：忽略审计字段及导航属性
        TypeAdapterConfig<InventoryAlertCreateDto, InventoryAlertEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // InventoryCheck -> InventoryCheckDto：时间字段名映射
        TypeAdapterConfig<InventoryCheckEntity, InventoryCheckDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // InventoryCheckCreateDto -> InventoryCheck：忽略审计字段及导航属性
        TypeAdapterConfig<InventoryCheckCreateDto, InventoryCheckEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // InventoryLog -> InventoryLogDto：时间字段名映射
        TypeAdapterConfig<InventoryLogEntity, InventoryLogDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // InventoryLogCreateDto -> InventoryLog：忽略审计字段及导航属性
        TypeAdapterConfig<InventoryLogCreateDto, InventoryLogEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product);

        // PurchaseReturn -> PurchaseReturnDto：时间字段名映射
        TypeAdapterConfig<PurchaseReturnEntity, PurchaseReturnDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // PurchaseReturnCreateDto -> PurchaseReturn：忽略审计字段及导航属性
        // Items 列表由 Mapster 自动映射，明细审计字段在 AppService 中设置
        TypeAdapterConfig<PurchaseReturnCreateDto, PurchaseReturnEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Supplier)
            .Ignore(d => d.PurchaseOrder)
            .Ignore(d => d.TotalQuantity)
            .Ignore(d => d.TotalRefundAmount);

        // StoredValueAccount -> StoredValueAccountDto：时间字段名映射
        TypeAdapterConfig<StoredValueAccountEntity, StoredValueAccountDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // StoredValueAccountCreateDto -> StoredValueAccount：忽略审计字段及导航属性
        TypeAdapterConfig<StoredValueAccountCreateDto, StoredValueAccountEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.Customer);

        // StoredValueLog -> StoredValueLogDto：时间字段名映射
        TypeAdapterConfig<StoredValueLogEntity, StoredValueLogDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // StoredValueLogCreateDto -> StoredValueLog：忽略审计字段及导航属性
        TypeAdapterConfig<StoredValueLogCreateDto, StoredValueLogEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // StoredValueRule -> StoredValueRuleDto：时间字段名映射
        TypeAdapterConfig<StoredValueRuleEntity, StoredValueRuleDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // StoredValueRuleCreateDto -> StoredValueRule：忽略审计字段
        TypeAdapterConfig<StoredValueRuleCreateDto, StoredValueRuleEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // PointsExchange -> PointsExchangeDto：时间字段名映射
        TypeAdapterConfig<PointsExchangeEntity, PointsExchangeDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // PointsExchangeCreateDto -> PointsExchange：忽略审计字段及导航属性
        TypeAdapterConfig<PointsExchangeCreateDto, PointsExchangeEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Customer);

        // TreatmentCard -> TreatmentCardDto：时间字段名映射
        TypeAdapterConfig<TreatmentCardEntity, TreatmentCardDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardCreateDto -> TreatmentCard：忽略审计字段（StoreId/StoreCode 由用户输入）
        TypeAdapterConfig<TreatmentCardCreateDto, TreatmentCardEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode);

        // TreatmentCardSale -> TreatmentCardSaleDto：时间字段名映射
        TypeAdapterConfig<TreatmentCardSaleEntity, TreatmentCardSaleDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardSaleCreateDto -> TreatmentCardSale：忽略审计字段及导航属性（Items 在 AppService 中手动构建）
        TypeAdapterConfig<TreatmentCardSaleCreateDto, TreatmentCardSaleEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.Card)
            .Ignore(d => d.Customer)
            .Ignore(d => d.Items);

        // TreatmentCardSaleItem -> TreatmentCardSaleItemDto：时间字段名映射
        TypeAdapterConfig<TreatmentCardSaleItemEntity, TreatmentCardSaleItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardSaleItemCreateDto -> TreatmentCardSaleItem：忽略审计字段及折算字段（折算单价在 AppService 中计算）
        TypeAdapterConfig<TreatmentCardSaleItemCreateDto, TreatmentCardSaleItemEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.Sale)
            .Ignore(d => d.Product)
            .Ignore(d => d.AllocatedUnitPrice)
            .Ignore(d => d.AllocatedTotalPrice);

        // TreatmentCardVerify -> TreatmentCardVerifyDto：时间字段名映射
        TypeAdapterConfig<TreatmentCardVerifyEntity, TreatmentCardVerifyDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TreatmentCardVerifyItem -> TreatmentCardVerifyItemDto：时间字段名映射
        TypeAdapterConfig<Bms.Store.Domain.Entities.TreatmentCardVerifyItem, TreatmentCardVerifyItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime);

        // TreatmentCardVerifyCreateDto -> TreatmentCardVerify：忽略审计字段、导航属性及后端计算字段
        TypeAdapterConfig<TreatmentCardVerifyCreateDto, TreatmentCardVerifyEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.CardSale)
            .Ignore(d => d.Items)
            .Ignore(d => d.VerifyAmount)
            .Ignore(d => d.OrderId)
            .Ignore(d => d.VerifyTimes)
            .Ignore(d => d.VerifyTime);

        // CourseCardItem -> CourseCardItemDto：时间字段名映射
        TypeAdapterConfig<CourseCardItemEntity, CourseCardItemDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // CourseCardItemCreateDto -> CourseCardItem：忽略审计字段及导航属性
        TypeAdapterConfig<CourseCardItemCreateDto, CourseCardItemEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.CourseCard)
            .Ignore(d => d.Product);

        // Supplier -> SupplierDto：时间字段名映射
        TypeAdapterConfig<SupplierEntity, SupplierDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ProductSupplier -> ProductSupplierDto：时间字段名映射
        // 关联展示字段（ProductCode/ProductName/SupplierCode/SupplierName）由 AppService 手动填充
        TypeAdapterConfig<ProductSupplierEntity, ProductSupplierDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // SupplierCreateDto -> Supplier：忽略审计字段
        TypeAdapterConfig<SupplierCreateDto, SupplierEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // Activity -> ActivityDto：时间字段名映射
        TypeAdapterConfig<ActivityEntity, ActivityDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // ActivityCreateDto -> Activity：忽略审计字段
        TypeAdapterConfig<ActivityCreateDto, ActivityEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // Technician -> TechnicianDto：时间字段名映射 + 技能分类 + 来源文本（只读派生字段需忽略写入）
        TypeAdapterConfig<TechnicianEntity, TechnicianDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Map(d => d.SkillCategoryIds, s => s.TechnicianSkills
                .Where(ts => ts.SkillCategory != null && !ts.SkillCategory.IsDeleted)
                .Select(ts => ts.SkillCategoryId).ToList())
            .Map(d => d.SkillCategoryNames, s => s.TechnicianSkills
                .Where(ts => ts.SkillCategory != null && !ts.SkillCategory.IsDeleted)
                .Select(ts => ts.SkillCategory.Name).ToList())
            .Ignore(d => d.SourceText);

        // TechnicianCreateDto -> Technician：忽略审计字段及导航属性
        TypeAdapterConfig<TechnicianCreateDto, TechnicianEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.TechnicianSkills);

        // TechnicianStatistic -> TechnicianStatisticDto：时间字段名映射
        TypeAdapterConfig<TechnicianStatisticEntity, TechnicianStatisticDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // TechnicianStatisticCreateDto -> TechnicianStatistic：忽略审计字段及导航属性
        TypeAdapterConfig<TechnicianStatisticCreateDto, TechnicianStatisticEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Technician);

        // DailyStat -> DailyStatDto：时间字段名映射
        TypeAdapterConfig<DailyStatEntity, DailyStatDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // DailyStatCreateDto -> DailyStat：忽略审计字段
        TypeAdapterConfig<DailyStatCreateDto, DailyStatEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // MonthlyStat -> MonthlyStatDto：时间字段名映射
        TypeAdapterConfig<MonthlyStatEntity, MonthlyStatDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // MonthlyStatCreateDto -> MonthlyStat：忽略审计字段
        TypeAdapterConfig<MonthlyStatCreateDto, MonthlyStatEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode);

        // SampleGiftReceive -> SampleGiftReceiveDto：时间字段名映射
        TypeAdapterConfig<SampleGiftReceiveEntity, SampleGiftReceiveDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime);

        // SampleGiftReceiveCreateDto -> SampleGiftReceive：忽略审计字段及导航属性
        TypeAdapterConfig<SampleGiftReceiveCreateDto, SampleGiftReceiveEntity>
            .NewConfig()
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.StoreId)
            .Ignore(d => d.StoreCode)
            .Ignore(d => d.Product)
            .Ignore(d => d.Customer)
            .Ignore(d => d.Activity);

        // ServiceProduct -> ServiceProductDto：时间字段名映射
        // MasterId（实体）映射到 ProductId（DTO），保持 DTO 契约不变
        TypeAdapterConfig<ServiceProductEntity, ServiceProductDto>
            .NewConfig()
            .Map(d => d.CreatedAt, s => s.CreatedTime)
            .Map(d => d.UpdatedAt, s => s.UpdatedTime)
            .Map(d => d.ProductId, s => s.MasterId);

        // ServiceProductCreateDto -> ServiceProduct：忽略审计字段及导航属性
        // ServiceProduct 已改为租户级（StoreTenantEntity），不再有 StoreId/StoreCode
        // ProductId（DTO）映射到 MasterId（实体），Product 导航改名为 Master
        TypeAdapterConfig<ServiceProductCreateDto, ServiceProductEntity>
            .NewConfig()
            .Map(d => d.MasterId, s => s.ProductId)
            .Ignore(d => d.Id)
            .Ignore(d => d.CreatedTime)
            .Ignore(d => d.UpdatedTime)
            .Ignore(d => d.IsDeleted)
            .Ignore(d => d.TenantId)
            .Ignore(d => d.TenantCode)
            .Ignore(d => d.Master);
    }
}
