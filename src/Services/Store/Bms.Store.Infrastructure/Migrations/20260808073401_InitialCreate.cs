using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bms_store");

            migrationBuilder.CreateTable(
                name: "Activities",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossStoreOperationLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequestIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerPhoneTail = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    HomeStoreId = table.Column<long>(type: "bigint", nullable: true),
                    HomeStoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsCrossStore = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedEntityId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedEntitySnapshot = table.Column<string>(type: "text", nullable: true),
                    FromCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    ToCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossStoreOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerDeleteLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginalPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    DeleteTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDeleteLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLevels",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTags",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailySettlements",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettlementDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SettlementTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    StoredValueRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    PointsDeductAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalRefund = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalStoredValueRecharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalStoredValueConsume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesOutboundCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TreatmentCardOutboundCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryLossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SampleGiftAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferOutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferInAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseReturnAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalGrossProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    TreatmentCardVerifyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfirmedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReversedBy = table.Column<long>(type: "bigint", nullable: true),
                    ReversedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReversedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyStats",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    StoredValueRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    PointsDeductAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesOutboundCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TreatmentCardOutboundCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryLossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SampleGiftAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferOutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferInAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchaseReturnAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StoredValueRecharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StoredValueConsume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConsumeCustomerCount = table.Column<int>(type: "integer", nullable: false),
                    NewCustomerCount = table.Column<int>(type: "integer", nullable: false),
                    AppointmentCount = table.Column<int>(type: "integer", nullable: false),
                    InventoryAlertCount = table.Column<int>(type: "integer", nullable: false),
                    TreatmentCardVerifyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentTypes",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Spec = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyStats",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatMonth = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CashRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    StoredValueRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    PointsDeductAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StoredValueRecharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StoredValueConsume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TreatmentCardVerifyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConsumeCustomerCount = table.Column<int>(type: "integer", nullable: false),
                    NewCustomerCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointsRules",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PointsRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DeductRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxDeductAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PointsValidityDays = table.Column<int>(type: "integer", nullable: true),
                    BirthdayDouble = table.Column<bool>(type: "boolean", nullable: false),
                    MinAmountThreshold = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PurchaseType = table.Column<int>(type: "integer", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCategories_SkillCategories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "bms_store",
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStoreId = table.Column<long>(type: "bigint", nullable: false),
                    FromStoreCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FromStoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToStoreId = table.Column<long>(type: "bigint", nullable: false),
                    ToStoreCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToStoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredValueRules",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiftAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiftRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Sort = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredValueRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessHours = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Area = table.Column<decimal>(type: "numeric", nullable: true),
                    ManagerId = table.Column<long>(type: "bigint", nullable: true),
                    ManagerName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    BusinessLicenseUrl = table.Column<string>(type: "text", nullable: true),
                    Remark = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreTenantSettings",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllowCrossStoreVerify = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTenantSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Contact = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCards",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceItems = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalTimes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValidityDays = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStores",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RealName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Birthday = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LevelId = table.Column<long>(type: "bigint", nullable: true),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalConsume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LastConsumeTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthorizationStatus = table.Column<int>(type: "integer", nullable: false),
                    AuthorizationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_CustomerLevels_LevelId",
                        column: x => x.LevelId,
                        principalSchema: "bms_store",
                        principalTable: "CustomerLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MaintenanceCycleDays = table.Column<int>(type: "integer", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalSchema: "bms_store",
                        principalTable: "EquipmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductMasters",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Specification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsSalable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMasters_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "bms_store",
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReturnNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: true),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalRefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VoucherImageUrl = table.Column<string>(type: "text", nullable: true),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "bms_store",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "bms_store",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianSkills",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: false),
                    SkillCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ProficiencyLevel = table.Column<int>(type: "integer", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianSkills_SkillCategories_SkillCategoryId",
                        column: x => x.SkillCategoryId,
                        principalSchema: "bms_store",
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianSkills_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalSchema: "bms_store",
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianStatistics",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: false),
                    StatDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ServiceCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    TotalTechnicianFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnCustomerCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianStatistics_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalSchema: "bms_store",
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BodyDataRecords",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    BodyFat = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    Bust = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    Waist = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    Hip = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyDataRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyDataRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumeLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    ConsumeTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumeLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerBeautyProfiles",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SkinType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sensitivity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HairType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllergyHistory = table.Column<string>(type: "text", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBeautyProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerBeautyProfiles_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPointsLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    BeforePoints = table.Column<int>(type: "integer", nullable: false),
                    AfterPoints = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPointsLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPointsLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPreferences",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    TechniquePressure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Temperature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MusicPreference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PreferredTechnicianId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPreferences_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTagLinks",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerTagLinks_CustomerTags_TagId",
                        column: x => x.TagId,
                        principalSchema: "bms_store",
                        principalTable: "CustomerTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerTagLinks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BackfillStatus = table.Column<int>(type: "integer", nullable: false),
                    ProductAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    PayMethod = table.Column<int>(type: "integer", nullable: true),
                    CashAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CashPayMethod = table.Column<int>(type: "integer", nullable: true),
                    StoredValueAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    PointsAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    OrderTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointsExchanges",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ExchangeType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: true),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PointsCost = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ExchangeTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsExchanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsExchanges_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceComparisonPhotos",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceItem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhotoDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PhotoType = table.Column<int>(type: "integer", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceComparisonPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceComparisonPhotos_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceReactions",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceItem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReactionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reaction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceReactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoredValueAccounts",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RealBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiftBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRecharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalGift = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalConsume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredValueAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredValueAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredValueLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RealAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiftAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BeforeBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AfterBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RealBalanceChange = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GiftBalanceChange = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BeforeRealBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AfterRealBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BeforeGiftBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AfterGiftBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    PayMethod = table.Column<int>(type: "integer", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredValueLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredValueLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCardSales",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalConsumedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingTimes = table.Column<int>(type: "integer", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCardSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCardSales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCardSales_TreatmentCards_CardId",
                        column: x => x.CardId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentMaintenanceReminders",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<long>(type: "bigint", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TargetMaintenanceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReminderType = table.Column<int>(type: "integer", nullable: false),
                    IsHandled = table.Column<bool>(type: "boolean", nullable: false),
                    HandledTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMaintenanceReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentMaintenanceReminders_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "bms_store",
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentMaintenances",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<long>(type: "bigint", nullable: false),
                    MaintenanceType = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Operator = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMaintenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentMaintenances_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "bms_store",
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LastPurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LowStockThreshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ExpiryAlertDays = table.Column<int>(type: "integer", nullable: true),
                    OverstockThreshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductMasters_MasterId",
                        column: x => x.MasterId,
                        principalSchema: "bms_store",
                        principalTable: "ProductMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProducts",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterId = table.Column<long>(type: "bigint", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    RequiredRoomType = table.Column<int>(type: "integer", nullable: true),
                    ApplicableSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProducts_ProductMasters_MasterId",
                        column: x => x.MasterId,
                        principalSchema: "bms_store",
                        principalTable: "ProductMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCardTransfers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardSaleId = table.Column<long>(type: "bigint", nullable: false),
                    FromCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ToCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TransferFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCardTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCardTransfers_Customers_FromCustomerId",
                        column: x => x.FromCustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCardTransfers_Customers_ToCustomerId",
                        column: x => x.ToCustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCardTransfers_TreatmentCardSales_CardSaleId",
                        column: x => x.CardSaleId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCardSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCardVerifies",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardSaleId = table.Column<long>(type: "bigint", nullable: false),
                    VerifyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VerifyTimes = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    VerifyTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    IsCrossStore = table.Column<bool>(type: "boolean", nullable: false),
                    ReverseStatus = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCardVerifies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCardVerifies_TreatmentCardSales_CardSaleId",
                        column: x => x.CardSaleId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCardSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AppointmentTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: true),
                    TechnicianSource = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    EquipmentId = table.Column<long>(type: "bigint", nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfirmTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompleteTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReminderStatus = table.Column<int>(type: "integer", nullable: false),
                    ReminderTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "bms_store",
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseCardItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseCardId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedTotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCardItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseCardItems_TreatmentCards_CourseCardId",
                        column: x => x.CourseCardId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAlerts",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AlertValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BatchId = table.Column<long>(type: "bigint", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessedRemark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProductionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ShelfLifeDays = table.Column<int>(type: "integer", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryChecks",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    BeforeQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiffQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CheckTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BatchNo = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DiffAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryChecks_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: true),
                    SupplierId = table.Column<long>(type: "bigint", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BeforeQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AfterQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RelatedId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    ActivityId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "bms_store",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLogs_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "bms_store",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: true),
                    TechnicianSource = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    EquipmentId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TechnicianFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ConsumableCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "bms_store",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceChangeLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangeTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceChangeLogs_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductSalesStats",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StatMonth = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    SalesCount = table.Column<int>(type: "integer", nullable: false),
                    SalesAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSalesStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSalesStats_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductSuppliers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    ReferencePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSuppliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "bms_store",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ShelfLifeDays = table.Column<int>(type: "integer", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "bms_store",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "bms_store",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseReturnId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalSchema: "bms_store",
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockTransferId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalSchema: "bms_store",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCardSaleItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedTotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCardSaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCardSaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCardSaleItems_TreatmentCardSales_SaleId",
                        column: x => x.SaleId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCardSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBoms",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceProductId = table.Column<long>(type: "bigint", nullable: false),
                    ConsumableProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceBoms_Products_ConsumableProductId",
                        column: x => x.ConsumableProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceBoms_ServiceProducts_ServiceProductId",
                        column: x => x.ServiceProductId,
                        principalSchema: "bms_store",
                        principalTable: "ServiceProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProductEquipments",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceProductId = table.Column<long>(type: "bigint", nullable: false),
                    EquipmentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProductEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProductEquipments_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalSchema: "bms_store",
                        principalTable: "EquipmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceProductEquipments_ServiceProducts_ServiceProductId",
                        column: x => x.ServiceProductId,
                        principalSchema: "bms_store",
                        principalTable: "ServiceProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentCardVerifyItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VerifyId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    VerifyTimes = table.Column<int>(type: "integer", nullable: false),
                    AllocatedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCardVerifyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCardVerifyItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCardVerifyItems_TreatmentCardVerifies_VerifyId",
                        column: x => x.VerifyId,
                        principalSchema: "bms_store",
                        principalTable: "TreatmentCardVerifies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SampleGiftReceives",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryBatchId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceiveTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActivityId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleGiftReceives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleGiftReceives_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "bms_store",
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SampleGiftReceives_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleGiftReceives_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalSchema: "bms_store",
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleGiftReceives_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemBatches",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemBatches_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "bms_store",
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderItemBatches_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "bms_store",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemBatches_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "bms_store",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_StartTime",
                schema: "bms_store",
                table: "Activities",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_StoreId",
                schema: "bms_store",
                table: "Activities",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate",
                schema: "bms_store",
                table: "Appointments",
                column: "AppointmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNo",
                schema: "bms_store",
                table: "Appointments",
                column: "AppointmentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                schema: "bms_store",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EquipmentId",
                schema: "bms_store",
                table: "Appointments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ProductId",
                schema: "bms_store",
                table: "Appointments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReminderStatus",
                schema: "bms_store",
                table: "Appointments",
                column: "ReminderStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RoomId",
                schema: "bms_store",
                table: "Appointments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                schema: "bms_store",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_StoreId",
                schema: "bms_store",
                table: "Appointments",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_BodyDataRecords_CustomerId",
                schema: "bms_store",
                table: "BodyDataRecords",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BodyDataRecords_RecordDate",
                schema: "bms_store",
                table: "BodyDataRecords",
                column: "RecordDate");

            migrationBuilder.CreateIndex(
                name: "IX_BodyDataRecords_TenantId_StoreId",
                schema: "bms_store",
                table: "BodyDataRecords",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumeLogs_ConsumeTime",
                schema: "bms_store",
                table: "ConsumeLogs",
                column: "ConsumeTime");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumeLogs_CustomerId",
                schema: "bms_store",
                table: "ConsumeLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumeLogs_OrderId",
                schema: "bms_store",
                table: "ConsumeLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumeLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "ConsumeLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseCardItems_CourseCardId",
                schema: "bms_store",
                table: "CourseCardItems",
                column: "CourseCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCardItems_ProductId",
                schema: "bms_store",
                table: "CourseCardItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseCardItems_TenantId_StoreId",
                schema: "bms_store",
                table: "CourseCardItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossStoreOperationLogs_TenantId_OperationTime",
                schema: "bms_store",
                table: "CrossStoreOperationLogs",
                columns: new[] { "TenantId", "OperationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CrossStoreOperationLogs_TenantId_OperationType_OperationTime",
                schema: "bms_store",
                table: "CrossStoreOperationLogs",
                columns: new[] { "TenantId", "OperationType", "OperationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBeautyProfiles_CustomerId",
                schema: "bms_store",
                table: "CustomerBeautyProfiles",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBeautyProfiles_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerBeautyProfiles",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeleteLogs_DeleteTime",
                schema: "bms_store",
                table: "CustomerDeleteLogs",
                column: "DeleteTime");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeleteLogs_OriginalCustomerId",
                schema: "bms_store",
                table: "CustomerDeleteLogs",
                column: "OriginalCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDeleteLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerDeleteLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLevels_Code",
                schema: "bms_store",
                table: "CustomerLevels",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLevels_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerLevels",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerLevels_Tenant_Level",
                schema: "bms_store",
                table: "CustomerLevels",
                columns: new[] { "TenantId", "Level" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPointsLogs_CustomerId",
                schema: "bms_store",
                table: "CustomerPointsLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPointsLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerPointsLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPointsLogs_Type",
                schema: "bms_store",
                table: "CustomerPointsLogs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPreferences_CustomerId",
                schema: "bms_store",
                table: "CustomerPreferences",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPreferences_PreferredTechnicianId",
                schema: "bms_store",
                table: "CustomerPreferences",
                column: "PreferredTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPreferences_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerPreferences",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AuthorizationStatus",
                schema: "bms_store",
                table: "Customers",
                column: "AuthorizationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_LevelId",
                schema: "bms_store",
                table: "Customers",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                schema: "bms_store",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_StoreId",
                schema: "bms_store",
                table: "Customers",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTagLinks_TagId",
                schema: "bms_store",
                table: "CustomerTagLinks",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTagLinks_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerTagLinks",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerTagLinks_Customer_Tag",
                schema: "bms_store",
                table: "CustomerTagLinks",
                columns: new[] { "CustomerId", "TagId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTags_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerTags",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerTags_Tenant_Store_Name",
                schema: "bms_store",
                table: "CustomerTags",
                columns: new[] { "TenantId", "StoreId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DailySettlements_TenantId_StoreId_SettlementDate",
                schema: "bms_store",
                table: "DailySettlements",
                columns: new[] { "TenantId", "StoreId", "SettlementDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyStats_TenantId_StoreId_StatDate",
                schema: "bms_store",
                table: "DailyStats",
                columns: new[] { "TenantId", "StoreId", "StatDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenanceReminders_EquipmentId",
                schema: "bms_store",
                table: "EquipmentMaintenanceReminders",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenanceReminders_EquipmentId_ReminderDate",
                schema: "bms_store",
                table: "EquipmentMaintenanceReminders",
                columns: new[] { "EquipmentId", "ReminderDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenanceReminders_IsHandled",
                schema: "bms_store",
                table: "EquipmentMaintenanceReminders",
                column: "IsHandled");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenanceReminders_ReminderDate",
                schema: "bms_store",
                table: "EquipmentMaintenanceReminders",
                column: "ReminderDate");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenanceReminders_TenantId_StoreId",
                schema: "bms_store",
                table: "EquipmentMaintenanceReminders",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_EquipmentId",
                schema: "bms_store",
                table: "EquipmentMaintenances",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_MaintenanceDate",
                schema: "bms_store",
                table: "EquipmentMaintenances",
                column: "MaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_MaintenanceType",
                schema: "bms_store",
                table: "EquipmentMaintenances",
                column: "MaintenanceType");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_NextMaintenanceDate",
                schema: "bms_store",
                table: "EquipmentMaintenances",
                column: "NextMaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_TenantId_StoreId",
                schema: "bms_store",
                table: "EquipmentMaintenances",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Code",
                schema: "bms_store",
                table: "Equipments",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_EquipmentTypeId",
                schema: "bms_store",
                table: "Equipments",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_NextMaintenanceDate",
                schema: "bms_store",
                table: "Equipments",
                column: "NextMaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Status",
                schema: "bms_store",
                table: "Equipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_TenantId_StoreId",
                schema: "bms_store",
                table: "Equipments",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_IsActive",
                schema: "bms_store",
                table: "EquipmentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_TenantId_StoreId",
                schema: "bms_store",
                table: "EquipmentTypes",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_TenantId_StoreId_Code",
                schema: "bms_store",
                table: "EquipmentTypes",
                columns: new[] { "TenantId", "StoreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                schema: "bms_store",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_TenantId_StoreId_ProductId",
                schema: "bms_store",
                table: "Inventories",
                columns: new[] { "TenantId", "StoreId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_AlertType",
                schema: "bms_store",
                table: "InventoryAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_IsProcessed",
                schema: "bms_store",
                table: "InventoryAlerts",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_ProductId",
                schema: "bms_store",
                table: "InventoryAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAlerts_TenantId_StoreId",
                schema: "bms_store",
                table: "InventoryAlerts",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_BatchNo",
                schema: "bms_store",
                table: "InventoryBatches",
                column: "BatchNo");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ExpirationDate",
                schema: "bms_store",
                table: "InventoryBatches",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductId",
                schema: "bms_store",
                table: "InventoryBatches",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_Status",
                schema: "bms_store",
                table: "InventoryBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_TenantId_StoreId_ProductId",
                schema: "bms_store",
                table: "InventoryBatches",
                columns: new[] { "TenantId", "StoreId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryBatches_Tenant_Store_BatchNo",
                schema: "bms_store",
                table: "InventoryBatches",
                columns: new[] { "TenantId", "StoreId", "BatchNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChecks_CheckTime",
                schema: "bms_store",
                table: "InventoryChecks",
                column: "CheckTime");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChecks_ProductId",
                schema: "bms_store",
                table: "InventoryChecks",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChecks_Status",
                schema: "bms_store",
                table: "InventoryChecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryChecks_TenantId_StoreId",
                schema: "bms_store",
                table: "InventoryChecks",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_ActivityId",
                schema: "bms_store",
                table: "InventoryLogs",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_ProductId",
                schema: "bms_store",
                table: "InventoryLogs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_SupplierId",
                schema: "bms_store",
                table: "InventoryLogs",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "InventoryLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_TenantId_StoreId_ActivityId",
                schema: "bms_store",
                table: "InventoryLogs",
                columns: new[] { "TenantId", "StoreId", "ActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLogs_Type",
                schema: "bms_store",
                table: "InventoryLogs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStats_TenantId_StoreId_StatMonth",
                schema: "bms_store",
                table: "MonthlyStats",
                columns: new[] { "TenantId", "StoreId", "StatMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatches_BatchId",
                schema: "bms_store",
                table: "OrderItemBatches",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatches_OrderId",
                schema: "bms_store",
                table: "OrderItemBatches",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatches_OrderItemId",
                schema: "bms_store",
                table: "OrderItemBatches",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatches_ProductId",
                schema: "bms_store",
                table: "OrderItemBatches",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemBatches_TenantId_StoreId_ProductId_ExpirationDate",
                schema: "bms_store",
                table: "OrderItemBatches",
                columns: new[] { "TenantId", "StoreId", "ProductId", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_EquipmentId",
                schema: "bms_store",
                table: "OrderItems",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "bms_store",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                schema: "bms_store",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_RoomId",
                schema: "bms_store",
                table: "OrderItems",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TechnicianId",
                schema: "bms_store",
                table: "OrderItems",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TenantId_StoreId",
                schema: "bms_store",
                table: "OrderItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BackfillStatus",
                schema: "bms_store",
                table: "Orders",
                column: "BackfillStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "bms_store",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                schema: "bms_store",
                table: "Orders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderTime",
                schema: "bms_store",
                table: "Orders",
                column: "OrderTime");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "bms_store",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_StoreId",
                schema: "bms_store",
                table: "Orders",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsExchanges_CustomerId",
                schema: "bms_store",
                table: "PointsExchanges",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsExchanges_ExchangeTime",
                schema: "bms_store",
                table: "PointsExchanges",
                column: "ExchangeTime");

            migrationBuilder.CreateIndex(
                name: "IX_PointsExchanges_ExchangeType",
                schema: "bms_store",
                table: "PointsExchanges",
                column: "ExchangeType");

            migrationBuilder.CreateIndex(
                name: "IX_PointsExchanges_TenantId_StoreId",
                schema: "bms_store",
                table: "PointsExchanges",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PointsRules_Status",
                schema: "bms_store",
                table: "PointsRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PointsRules_TenantId_StoreId",
                schema: "bms_store",
                table: "PointsRules",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceChangeLogs_ChangeTime",
                schema: "bms_store",
                table: "PriceChangeLogs",
                column: "ChangeTime");

            migrationBuilder.CreateIndex(
                name: "IX_PriceChangeLogs_ProductId",
                schema: "bms_store",
                table: "PriceChangeLogs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceChangeLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "PriceChangeLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Tenant",
                schema: "bms_store",
                table: "ProductCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductCategories_Tenant_Code_Deleted",
                schema: "bms_store",
                table: "ProductCategories",
                columns: new[] { "TenantId", "Code", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductMasters_Category",
                schema: "bms_store",
                table: "ProductMasters",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMasters_Tenant",
                schema: "bms_store",
                table: "ProductMasters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductMasters_Tenant_Code_Deleted",
                schema: "bms_store",
                table: "ProductMasters",
                columns: new[] { "TenantId", "Code", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Master",
                schema: "bms_store",
                table: "Products",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Tenant_Store",
                schema: "bms_store",
                table: "Products",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_Products_Tenant_Store_Master_Deleted",
                schema: "bms_store",
                table: "Products",
                columns: new[] { "TenantId", "StoreId", "MasterId", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_ProductId",
                schema: "bms_store",
                table: "ProductSalesStats",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_ProductType",
                schema: "bms_store",
                table: "ProductSalesStats",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_StatDate",
                schema: "bms_store",
                table: "ProductSalesStats",
                column: "StatDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_StatMonth",
                schema: "bms_store",
                table: "ProductSalesStats",
                column: "StatMonth");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId",
                schema: "bms_store",
                table: "ProductSalesStats",
                columns: new[] { "TenantId", "StoreId", "StatDate", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_ProductId",
                schema: "bms_store",
                table: "ProductSuppliers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_SupplierId",
                schema: "bms_store",
                table: "ProductSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSuppliers_TenantId_StoreId_IsDefault",
                schema: "bms_store",
                table: "ProductSuppliers",
                columns: new[] { "TenantId", "StoreId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "UX_ProductSuppliers_Product_Supplier_Tenant_Store",
                schema: "bms_store",
                table: "ProductSuppliers",
                columns: new[] { "ProductId", "SupplierId", "TenantId", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId",
                schema: "bms_store",
                table: "PurchaseOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                schema: "bms_store",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_SupplierId",
                schema: "bms_store",
                table: "PurchaseOrderItems",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TenantId_StoreId",
                schema: "bms_store",
                table: "PurchaseOrderItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderDate",
                schema: "bms_store",
                table: "PurchaseOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNo",
                schema: "bms_store",
                table: "PurchaseOrders",
                column: "OrderNo");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseType",
                schema: "bms_store",
                table: "PurchaseOrders",
                column: "PurchaseType");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                schema: "bms_store",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_StoreId",
                schema: "bms_store",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseOrders_Tenant_Store_OrderNo",
                schema: "bms_store",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "StoreId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_ProductId",
                schema: "bms_store",
                table: "PurchaseReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_PurchaseReturnId",
                schema: "bms_store",
                table: "PurchaseReturnItems",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_TenantId_StoreId",
                schema: "bms_store",
                table: "PurchaseReturnItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PurchaseOrderId",
                schema: "bms_store",
                table: "PurchaseReturns",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNo",
                schema: "bms_store",
                table: "PurchaseReturns",
                column: "ReturnNo");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnTime",
                schema: "bms_store",
                table: "PurchaseReturns",
                column: "ReturnTime");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_SupplierId",
                schema: "bms_store",
                table: "PurchaseReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId_StoreId",
                schema: "bms_store",
                table: "PurchaseReturns",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseReturns_Tenant_Store_ReturnNo",
                schema: "bms_store",
                table: "PurchaseReturns",
                columns: new[] { "TenantId", "StoreId", "ReturnNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Code",
                schema: "bms_store",
                table: "Rooms",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomType",
                schema: "bms_store",
                table: "Rooms",
                column: "RoomType");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Status",
                schema: "bms_store",
                table: "Rooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId_StoreId",
                schema: "bms_store",
                table: "Rooms",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_ActivityId",
                schema: "bms_store",
                table: "SampleGiftReceives",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_CustomerId",
                schema: "bms_store",
                table: "SampleGiftReceives",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_InventoryBatchId",
                schema: "bms_store",
                table: "SampleGiftReceives",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_ProductId",
                schema: "bms_store",
                table: "SampleGiftReceives",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_ReceiveTime",
                schema: "bms_store",
                table: "SampleGiftReceives",
                column: "ReceiveTime");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftReceives_TenantId_StoreId",
                schema: "bms_store",
                table: "SampleGiftReceives",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBoms_ConsumableProductId",
                schema: "bms_store",
                table: "ServiceBoms",
                column: "ConsumableProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBoms_ServiceProductId",
                schema: "bms_store",
                table: "ServiceBoms",
                column: "ServiceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBoms_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceBoms",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_CustomerId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_OrderId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_PhotoDate",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                column: "PhotoDate");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_PhotoType",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                column: "PhotoType");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductEquipments_EquipmentTypeId",
                schema: "bms_store",
                table: "ServiceProductEquipments",
                column: "EquipmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductEquipments_ServiceProductId",
                schema: "bms_store",
                table: "ServiceProductEquipments",
                column: "ServiceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductEquipments_ServiceProductId_EquipmentTypeId",
                schema: "bms_store",
                table: "ServiceProductEquipments",
                columns: new[] { "ServiceProductId", "EquipmentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductEquipments_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceProductEquipments",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProducts_MasterId",
                schema: "bms_store",
                table: "ServiceProducts",
                column: "MasterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProducts_Tenant",
                schema: "bms_store",
                table: "ServiceProducts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceProducts_Tenant_Master",
                schema: "bms_store",
                table: "ServiceProducts",
                columns: new[] { "TenantId", "MasterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReactions_CustomerId",
                schema: "bms_store",
                table: "ServiceReactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReactions_OrderId",
                schema: "bms_store",
                table: "ServiceReactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReactions_ReactionDate",
                schema: "bms_store",
                table: "ServiceReactions",
                column: "ReactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReactions_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceReactions",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Code",
                schema: "bms_store",
                table: "SkillCategories",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_ParentId",
                schema: "bms_store",
                table: "SkillCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Status",
                schema: "bms_store",
                table: "SkillCategories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_TenantId_StoreId",
                schema: "bms_store",
                table: "SkillCategories",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_ProductId",
                schema: "bms_store",
                table: "StockTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_StockTransferId",
                schema: "bms_store",
                table: "StockTransferItems",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_TenantId_StoreId",
                schema: "bms_store",
                table: "StockTransferItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromStoreId",
                schema: "bms_store",
                table: "StockTransfers",
                column: "FromStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Status",
                schema: "bms_store",
                table: "StockTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TenantId_StoreId",
                schema: "bms_store",
                table: "StockTransfers",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToStoreId",
                schema: "bms_store",
                table: "StockTransfers",
                column: "ToStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferDate",
                schema: "bms_store",
                table: "StockTransfers",
                column: "TransferDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNo",
                schema: "bms_store",
                table: "StockTransfers",
                column: "TransferNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueAccounts_CustomerId",
                schema: "bms_store",
                table: "StoredValueAccounts",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueAccounts_TenantId",
                schema: "bms_store",
                table: "StoredValueAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueAccounts_TenantId_StoreId",
                schema: "bms_store",
                table: "StoredValueAccounts",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueLogs_CustomerId",
                schema: "bms_store",
                table: "StoredValueLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "StoredValueLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueLogs_Type",
                schema: "bms_store",
                table: "StoredValueLogs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueRules_Code",
                schema: "bms_store",
                table: "StoredValueRules",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueRules_TenantId_StoreId",
                schema: "bms_store",
                table: "StoredValueRules",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Code",
                schema: "bms_store",
                table: "Stores",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Status",
                schema: "bms_store",
                table: "Stores",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_TenantId",
                schema: "bms_store",
                table: "Stores",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_StoreTenantSettings_Tenant",
                schema: "bms_store",
                table: "StoreTenantSettings",
                column: "TenantId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                schema: "bms_store",
                table: "Suppliers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Status",
                schema: "bms_store",
                table: "Suppliers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_StoreId",
                schema: "bms_store",
                table: "Suppliers",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_Suppliers_Tenant_Code_Deleted",
                schema: "bms_store",
                table: "Suppliers",
                columns: new[] { "TenantId", "Code", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Phone",
                schema: "bms_store",
                table: "Technicians",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Source",
                schema: "bms_store",
                table: "Technicians",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Status",
                schema: "bms_store",
                table: "Technicians",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_TenantId_StoreId",
                schema: "bms_store",
                table: "Technicians",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSkills_SkillCategoryId",
                schema: "bms_store",
                table: "TechnicianSkills",
                column: "SkillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSkills_TechnicianId_SkillCategoryId",
                schema: "bms_store",
                table: "TechnicianSkills",
                columns: new[] { "TechnicianId", "SkillCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSkills_TenantId",
                schema: "bms_store",
                table: "TechnicianSkills",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianStatistics_StatDate",
                schema: "bms_store",
                table: "TechnicianStatistics",
                column: "StatDate");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianStatistics_TechnicianId",
                schema: "bms_store",
                table: "TechnicianStatistics",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianStatistics_TenantId_StoreId_StatDate",
                schema: "bms_store",
                table: "TechnicianStatistics",
                columns: new[] { "TenantId", "StoreId", "StatDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCards_Code",
                schema: "bms_store",
                table: "TreatmentCards",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCards_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCards",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSaleItems_ProductId",
                schema: "bms_store",
                table: "TreatmentCardSaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSaleItems_SaleId",
                schema: "bms_store",
                table: "TreatmentCardSaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSaleItems_TenantId_SaleId",
                schema: "bms_store",
                table: "TreatmentCardSaleItems",
                columns: new[] { "TenantId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSaleItems_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCardSaleItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSales_CardId",
                schema: "bms_store",
                table: "TreatmentCardSales",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSales_CustomerId",
                schema: "bms_store",
                table: "TreatmentCardSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSales_Status",
                schema: "bms_store",
                table: "TreatmentCardSales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSales_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCardSales",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardTransfers_CardSaleId",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                column: "CardSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardTransfers_FromCustomerId",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                column: "FromCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardTransfers_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardTransfers_ToCustomerId",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                column: "ToCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardTransfers_TransferDate",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                column: "TransferDate");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifies_CardSaleId",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                column: "CardSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifies_OrderId",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifies_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifies_VerifyTime",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                column: "VerifyTime");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifyItems_ProductId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifyItems_TenantId_StoreId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifyItems_TenantId_VerifyId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                columns: new[] { "TenantId", "VerifyId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifyItems_VerifyId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                column: "VerifyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStores_Tenant_Store",
                schema: "bms_store",
                table: "UserStores",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStores_Tenant_User",
                schema: "bms_store",
                table: "UserStores",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "UX_UserStores_UserId_StoreId",
                schema: "bms_store",
                table: "UserStores",
                columns: new[] { "UserId", "StoreId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "BodyDataRecords",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ConsumeLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CourseCardItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CrossStoreOperationLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerBeautyProfiles",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerDeleteLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerPointsLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerPreferences",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerTagLinks",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "DailySettlements",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "DailyStats",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "EquipmentMaintenanceReminders",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "EquipmentMaintenances",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Inventories",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "InventoryAlerts",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "InventoryChecks",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "InventoryLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "MonthlyStats",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "OrderItemBatches",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PointsExchanges",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PointsRules",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PriceChangeLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ProductSalesStats",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ProductSuppliers",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PurchaseReturnItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "SampleGiftReceives",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ServiceBoms",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ServiceComparisonPhotos",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ServiceProductEquipments",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ServiceReactions",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StockTransferItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StoredValueAccounts",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StoredValueLogs",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StoredValueRules",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Stores",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StoreTenantSettings",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TechnicianSkills",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TechnicianStatistics",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCardSaleItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCardTransfers",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCardVerifyItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "UserStores",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerTags",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Equipments",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PurchaseReturns",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Activities",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "InventoryBatches",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ServiceProducts",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "SkillCategories",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Technicians",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCardVerifies",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "EquipmentTypes",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCardSales",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ProductMasters",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "TreatmentCards",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "ProductCategories",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "CustomerLevels",
                schema: "bms_store");
        }
    }
}
