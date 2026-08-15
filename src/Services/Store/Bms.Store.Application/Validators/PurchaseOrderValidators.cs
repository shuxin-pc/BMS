using FluentValidation;
using Bms.Store.Application.Dtos.PurchaseOrders;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建采购订单请求验证器
/// 采购单号由后端自动生成，供应商ID下沉到明细级
/// </summary>
public class PurchaseOrderCreateDtoValidator : AbstractValidator<PurchaseOrderCreateDto>
{
    public PurchaseOrderCreateDtoValidator()
    {
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("采购日期不能为空");
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0).WithMessage("采购总金额必须大于等于0");
        RuleFor(x => x.PurchaseType).Must(t => t == 1 || t == 2).WithMessage("采购类型只能为1(零售商品采购)或2(耗材采购)");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SupplierId).GreaterThan(0).WithMessage("供应商ID必须大于0");
        });
    }
}
