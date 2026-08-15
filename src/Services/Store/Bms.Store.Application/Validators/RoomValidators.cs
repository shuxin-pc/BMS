using FluentValidation;
using Bms.Store.Application.Dtos.Rooms;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建房间床位请求验证器
/// </summary>
public class RoomCreateDtoValidator : AbstractValidator<RoomCreateDto>
{
    public RoomCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名称不能为空")
            .MaximumLength(100).WithMessage("名称最多100个字符");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("编码不能为空")
            .MaximumLength(50).WithMessage("编码最多50个字符");
        RuleFor(x => x.RoomType)
            .Must(t => t == 1 || t == 2).WithMessage("类型只能为1(房间)或2(床位)");
        RuleFor(x => x.Status)
            .Must(s => s == 0 || s == 1).WithMessage("状态只能为0(停用)或1(启用)");
    }
}

/// <summary>
/// 更新房间床位请求验证器
/// </summary>
public class RoomUpdateDtoValidator : AbstractValidator<RoomUpdateDto>
{
    public RoomUpdateDtoValidator()
    {
        Include(new RoomCreateDtoValidator());
    }
}
