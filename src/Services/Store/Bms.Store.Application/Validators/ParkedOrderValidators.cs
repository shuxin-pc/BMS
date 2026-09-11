using System.Text.Json;
using FluentValidation;
using Bms.Store.Application.Dtos.ParkedOrders;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 挂单创建验证器
/// 校验购物车 JSON 为合法数组，防止存入非法/超大数据
/// </summary>
public class ParkedOrderCreateDtoValidator : AbstractValidator<ParkedOrderCreateDto>
{
    public ParkedOrderCreateDtoValidator()
    {
        RuleFor(x => x.CartJson)
            .NotEmpty().WithMessage("购物车为空")
            .Must(IsValidCartJsonArray).WithMessage("购物车数据格式非法");
        RuleFor(x => x.Remark)
            .MaximumLength(200).WithMessage("备注不能超过200字");
        RuleFor(x => x.CustomerName)
            .MaximumLength(50).WithMessage("客户姓名不能超过50字");
    }

    /// <summary>
    /// 校验购物车 JSON 为合法数组（元素为对象），单挂单购物车行数上限 200 防御
    /// </summary>
    private static bool IsValidCartJsonArray(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                && doc.RootElement.GetArrayLength() <= 200;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
