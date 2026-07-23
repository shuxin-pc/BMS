using FluentValidation;
using Bms.System.Application.Dtos.Messages;

namespace Bms.System.Application.Validators;

/// <summary>
/// 发送消息参数验证器
/// </summary>
public class MessageSendDtoValidator : AbstractValidator<MessageSendDto>
{
    public MessageSendDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("消息标题不能为空")
            .MaximumLength(200).WithMessage("消息标题最多200个字符");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("消息内容不能为空")
            .MaximumLength(2000).WithMessage("消息内容最多2000个字符");

        RuleFor(x => x.Category)
            .InclusiveBetween(1, 3).WithMessage("分类只能是1(系统)、2(业务)、3(公告)");

        RuleFor(x => x.TargetType)
            .InclusiveBetween(1, 5).WithMessage("目标类型只能是1-5");

        // 按租户发送时 TargetIds 为租户ID列表，其他类型为目标ID列表
        RuleFor(x => x.TargetIds)
            .NotEmpty().WithMessage("目标ID列表不能为空（全员除外）")
            .When(x => x.TargetType != 5); // 5=全员时可为空

        RuleFor(x => x.TargetUrl)
            .MaximumLength(500).WithMessage("跳转链接最多500个字符")
            .When(x => !string.IsNullOrEmpty(x.TargetUrl));
    }
}
