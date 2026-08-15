using FluentValidation;
using Bms.Store.Application.Dtos.PurchaseReturns;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 采购退货明细验证器
/// </summary>
public class PurchaseReturnItemCreateDtoValidator : AbstractValidator<PurchaseReturnItemCreateDto>
{
    public PurchaseReturnItemCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("退货数量必须大于0");
        RuleFor(x => x.RefundAmount).GreaterThanOrEqualTo(0).WithMessage("退款金额必须大于等于0");
        RuleFor(x => x.BatchNo).MaximumLength(50).WithMessage("批次号最多50个字符");
        RuleFor(x => x.Remark).MaximumLength(500).WithMessage("备注最多500个字符");
    }
}

/// <summary>
/// 创建采购退货请求验证器
/// 支持一次退回多种商品，明细通过 Items 列表传入，至少包含一条明细
/// </summary>
public class PurchaseReturnCreateDtoValidator : AbstractValidator<PurchaseReturnCreateDto>
{
    public PurchaseReturnCreateDtoValidator()
    {
        // 退货单号由后端在事务内自动生成（PR{yyyyMMdd}{序号}），创建时可不传；
        // 编辑时由前端回填原值，长度上限保持 50
        RuleFor(x => x.ReturnNo).MaximumLength(50).WithMessage("退货单号最多50个字符");
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("供应商ID必须大于0");
        // 关联采购订单ID可选，传入时必须大于 0
        RuleFor(x => x.PurchaseOrderId)
            .GreaterThan(0).When(x => x.PurchaseOrderId.HasValue)
            .WithMessage("采购订单ID必须大于0");
        RuleFor(x => x.Items).NotEmpty().WithMessage("退货明细不能为空");
        RuleForEach(x => x.Items).SetValidator(new PurchaseReturnItemCreateDtoValidator());
    }
}

/// <summary>
/// 更新采购退货请求验证器
/// </summary>
public class PurchaseReturnUpdateDtoValidator : AbstractValidator<PurchaseReturnUpdateDto>
{
    public PurchaseReturnUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new PurchaseReturnCreateDtoValidator());
    }
}
