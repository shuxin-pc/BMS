using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Bms.Store.Application.Services;
using Bms.Store.Application.Services.Resources;
using Bms.Store.Application.Validators;

namespace Bms.Store.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册应用服务
        services.AddScoped<IStoreAppService, StoreAppService>();
        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductCategoryAppService, ProductCategoryAppService>();
        services.AddScoped<IServiceBomAppService, ServiceBomAppService>();
        services.AddScoped<IRoomAppService, RoomAppService>();
        services.AddScoped<IEquipmentAppService, EquipmentAppService>();
        services.AddScoped<IInventoryBatchAppService, InventoryBatchAppService>();
        services.AddScoped<IStockTransferAppService, StockTransferAppService>();
        services.AddScoped<IStockTransferItemAppService, StockTransferItemAppService>();
        services.AddScoped<IProductSalesStatAppService, ProductSalesStatAppService>();
        services.AddScoped<ISkillCategoryAppService, SkillCategoryAppService>();
        services.AddScoped<IDailySettlementAppService, DailySettlementAppService>();
        services.AddScoped<ITreatmentCardTransferAppService, TreatmentCardTransferAppService>();
        services.AddScoped<IEquipmentMaintenanceAppService, EquipmentMaintenanceAppService>();
        services.AddScoped<IPurchaseOrderAppService, PurchaseOrderAppService>();
        services.AddScoped<IPurchaseOrderItemAppService, PurchaseOrderItemAppService>();
        services.AddScoped<IPointsRuleAppService, PointsRuleAppService>();
        services.AddScoped<ICustomerPreferenceAppService, CustomerPreferenceAppService>();
        services.AddScoped<IServiceReactionAppService, ServiceReactionAppService>();
        services.AddScoped<IServiceComparisonPhotoAppService, ServiceComparisonPhotoAppService>();
        services.AddScoped<IBodyDataRecordAppService, BodyDataRecordAppService>();

        // 客户模块
        services.AddScoped<ICustomerAppService, CustomerAppService>();
        services.AddScoped<ICustomerLevelAppService, CustomerLevelAppService>();
        services.AddScoped<ICustomerBeautyProfileAppService, CustomerBeautyProfileAppService>();
        services.AddScoped<ICustomerPointsLogAppService, CustomerPointsLogAppService>();
        services.AddScoped<IConsumeLogAppService, ConsumeLogAppService>();

        // 订单与预约模块
        services.AddScoped<IOrderAppService, OrderAppService>();
        services.AddScoped<IOrderItemAppService, OrderItemAppService>();
        services.AddScoped<IAppointmentAppService, AppointmentAppService>();
        services.AddScoped<IPriceChangeLogAppService, PriceChangeLogAppService>();

        // 库存与采购退货模块
        services.AddScoped<IInventoryAppService, InventoryAppService>();
        services.AddScoped<IInventoryAlertAppService, InventoryAlertAppService>();
        services.AddScoped<IInventoryCheckAppService, InventoryCheckAppService>();
        services.AddScoped<IInventoryLogAppService, InventoryLogAppService>();
        services.AddScoped<IPurchaseReturnAppService, PurchaseReturnAppService>();

        // 储值、积分与疗程卡模块
        services.AddScoped<IStoredValueAccountAppService, StoredValueAccountAppService>();
        services.AddScoped<IStoredValueLogAppService, StoredValueLogAppService>();
        services.AddScoped<IStoredValueRuleAppService, StoredValueRuleAppService>();
        services.AddScoped<IPointsExchangeAppService, PointsExchangeAppService>();
        services.AddScoped<ITreatmentCardAppService, TreatmentCardAppService>();

        // 疗程卡销售核销、供应商与技师模块
        services.AddScoped<ITreatmentCardSaleAppService, TreatmentCardSaleAppService>();
        services.AddScoped<ITreatmentCardVerifyAppService, TreatmentCardVerifyAppService>();
        services.AddScoped<ICourseCardItemAppService, CourseCardItemAppService>();
        services.AddScoped<ISupplierAppService, SupplierAppService>();
        services.AddScoped<ITechnicianAppService, TechnicianAppService>();
        services.AddScoped<ITechnicianPermissionService, TechnicianPermissionService>();

        // 资源冲突检测与可用性查询（技师/房间/设备）
        services.AddScoped<IResourceConflictCheckService, ResourceConflictCheckService>();
        services.AddScoped<IResourceAvailabilityService, ResourceAvailabilityService>();

        // 统计、营销与样品赠品模块
        services.AddScoped<ITechnicianStatisticAppService, TechnicianStatisticAppService>();
        services.AddScoped<IDailyStatAppService, DailyStatAppService>();
        services.AddScoped<IMonthlyStatAppService, MonthlyStatAppService>();
        services.AddScoped<IDashboardAppService, DashboardAppService>();
        services.AddScoped<IProductExpirySalesStatAppService, ProductExpirySalesStatAppService>();

        // 样品赠品档案、出库/领用模块
        services.AddScoped<ISampleGiftAppService, SampleGiftAppService>();
        services.AddScoped<ISampleGiftOutAppService, SampleGiftOutAppService>();
        services.AddScoped<ISampleGiftReceiveAppService, SampleGiftReceiveAppService>();
        services.AddScoped<IServiceProductAppService, ServiceProductAppService>();

        // 注册 FluentValidation
        services.AddValidatorsFromAssemblyContaining<StoreCreateDtoValidator>();
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
