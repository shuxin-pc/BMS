using FluentValidation;
using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class ServiceComparisonPhotoCreateDtoValidator : AbstractValidator<ServiceComparisonPhotoCreateDto>
{
    /// <summary>
    /// 单条记录（即服务前或服务后其中一侧）允许的最大照片数
    /// </summary>
    private const int MaxPhotoCount = 4;

    public ServiceComparisonPhotoCreateDtoValidator(IExternalUrlValidator externalUrlValidator)
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.OrderId)
            .Must(id => !id.HasValue || id.Value > 0).WithMessage("关联订单不合法");
        RuleFor(x => x.ProductId)
            .Must(id => !id.HasValue || id.Value > 0).WithMessage("服务项目不合法");
        RuleFor(x => x.Items).NotEmpty().WithMessage("请至少上传一张照片");
        RuleFor(x => x.Items)
            .Must(items => items.Count <= MaxPhotoCount)
            .WithMessage($"照片数量不能超过 {MaxPhotoCount} 张");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            // Id 为空表示新增照片，此时必须携带照片来源；有 Id 表示保留已有照片，无需重复回传
            item.RuleFor(i => i.ObjectKey)
                .NotEmpty().When(i => !i.Id.HasValue).WithMessage("照片来源不能为空");
            // 带协议头的一律按外链处理并只放行 http/https，
            // 否则 javascript:、data: 等伪协议会被当成 objectKey 存库，读取时原样吐给前端形成 XSS
            item.RuleFor(i => i.ObjectKey)
                .Must(v => string.IsNullOrWhiteSpace(v)
                           || !externalUrlValidator.LooksLikeExternalUrl(v)
                           || externalUrlValidator.IsAllowedExternalUrl(v))
                .WithMessage("图片链接协议不合法");
        });
        RuleFor(x => x.PhotoType)
            .Must(t => t == 1 || t == 2).WithMessage("照片类型只能为1(服务前)或2(服务后)");
    }
}

public class ServiceComparisonPhotoUpdateDtoValidator : AbstractValidator<ServiceComparisonPhotoUpdateDto>
{
    public ServiceComparisonPhotoUpdateDtoValidator(IExternalUrlValidator externalUrlValidator)
    {
        Include(new ServiceComparisonPhotoCreateDtoValidator(externalUrlValidator));
    }
}
