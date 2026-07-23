using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class ServiceComparisonPhotoCreateDtoValidator : AbstractValidator<ServiceComparisonPhotoCreateDto>
{
    public ServiceComparisonPhotoCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("对比照片必须关联服务订单");
        RuleFor(x => x.PhotoUrl).NotEmpty().WithMessage("照片URL不能为空");
        RuleFor(x => x.PhotoType)
            .Must(t => t == 1 || t == 2).WithMessage("照片类型只能为1(服务前)或2(服务后)");
    }
}

public class ServiceComparisonPhotoUpdateDtoValidator : AbstractValidator<ServiceComparisonPhotoUpdateDto>
{
    public ServiceComparisonPhotoUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new ServiceComparisonPhotoCreateDtoValidator());
    }
}
