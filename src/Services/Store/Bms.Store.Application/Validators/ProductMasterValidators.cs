using FluentValidation;
using Bms.Store.Application.Dtos.Products;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建商品主档请求验证器（Master 字段全租户生效，校验必填项与长度）
/// 服务商品（Type=2）子表字段同步校验
/// </summary>
public class ProductMasterCreateDtoValidator : AbstractValidator<ProductMasterCreateDto>
{
    public ProductMasterCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("商品编码不能为空")
            .MaximumLength(50).WithMessage("商品编码最多50个字符");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("商品名称不能为空")
            .MaximumLength(100).WithMessage("商品名称最多100个字符");

        // 商品类型（1:实物 2:服务 3:耗材 4:样品 5:赠品）
        RuleFor(x => x.Type)
            .InclusiveBetween(1, 5).WithMessage("商品类型只能为1(实物)2(服务)3(耗材)4(样品)5(赠品)");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("请选择商品分类");

        RuleFor(x => x.Unit)
            .MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Unit))
            .WithMessage("单位最多20个字符");

        RuleFor(x => x.Specification)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Specification))
            .WithMessage("规格最多100个字符");

        RuleFor(x => x.Brand)
            .MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Brand))
            .WithMessage("品牌最多50个字符");

        RuleFor(x => x.Remark)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Remark))
            .WithMessage("备注最多500个字符");

        // ========== 服务商品（Type=2）子表字段校验 ==========
        // 服务时长必填且大于0
        RuleFor(x => x.Duration)
            .GreaterThan(0).When(x => x.Type == 2)
            .WithMessage("服务商品的服务时长必须大于0");

        // 所需房型只能为 1(房间) 或 2(床位)，未设置时不校验
        RuleFor(x => x.RequiredRoomType)
            .Must(v => v == null || v == 1 || v == 2).When(x => x.Type == 2)
            .WithMessage("所需房型只能为1(房间)或2(床位)");

        // 设备类型ID列表各元素必须大于0
        RuleForEach(x => x.EquipmentTypeIds)
            .GreaterThan(0).When(x => x.Type == 2 && x.EquipmentTypeIds != null && x.EquipmentTypeIds.Any())
            .WithMessage("设备类型ID必须大于0");
    }
}

/// <summary>
/// 更新商品主档请求验证器（Id + 复用 Create 校验规则）
/// </summary>
public class ProductMasterUpdateDtoValidator : AbstractValidator<ProductMasterUpdateDto>
{
    public ProductMasterUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("商品主档ID无效");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("商品编码不能为空")
            .MaximumLength(50).WithMessage("商品编码最多50个字符");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("商品名称不能为空")
            .MaximumLength(100).WithMessage("商品名称最多100个字符");

        RuleFor(x => x.Type)
            .InclusiveBetween(1, 5).WithMessage("商品类型只能为1(实物)2(服务)3(耗材)4(样品)5(赠品)");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("请选择商品分类");

        RuleFor(x => x.Unit)
            .MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Unit))
            .WithMessage("单位最多20个字符");

        RuleFor(x => x.Specification)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Specification))
            .WithMessage("规格最多100个字符");

        RuleFor(x => x.Brand)
            .MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Brand))
            .WithMessage("品牌最多50个字符");

        RuleFor(x => x.Remark)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Remark))
            .WithMessage("备注最多500个字符");

        // ========== 服务商品（Type=2）子表字段校验 ==========
        RuleFor(x => x.Duration)
            .GreaterThan(0).When(x => x.Type == 2)
            .WithMessage("服务商品的服务时长必须大于0");

        RuleFor(x => x.RequiredRoomType)
            .Must(v => v == null || v == 1 || v == 2).When(x => x.Type == 2)
            .WithMessage("所需房型只能为1(房间)或2(床位)");

        RuleForEach(x => x.EquipmentTypeIds)
            .GreaterThan(0).When(x => x.Type == 2 && x.EquipmentTypeIds != null && x.EquipmentTypeIds.Any())
            .WithMessage("设备类型ID必须大于0");
    }
}

/// <summary>
/// 统一配置门店档案 Store 字段请求验证器（对应设计文档 7.2/7.3 节）
/// 支持单选/多选门店，校验 MasterId、StoreIds、Price、Status
/// </summary>
public class ProductStoreBatchConfigDtoValidator : AbstractValidator<ProductStoreBatchConfigDto>
{
    public ProductStoreBatchConfigDtoValidator()
    {
        RuleFor(x => x.MasterId)
            .GreaterThan(0).WithMessage("商品主档ID无效");

        // 至少选择一个门店，各门店ID必须大于0
        RuleFor(x => x.StoreIds)
            .NotEmpty().WithMessage("请至少选择一个门店");

        RuleForEach(x => x.StoreIds)
            .GreaterThan(0).WithMessage("门店ID必须大于0");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("售价必须大于等于0");

        // 上架状态（1:上架 2:下架）
        RuleFor(x => x.Status)
            .Must(s => s == 1 || s == 2).WithMessage("上架状态只能为1(上架)或2(下架)");

        RuleFor(x => x.Remark)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Remark))
            .WithMessage("备注最多500个字符");
    }
}
