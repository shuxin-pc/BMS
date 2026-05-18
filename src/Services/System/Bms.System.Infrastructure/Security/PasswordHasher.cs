using Bms.System.Domain.Interfaces;

namespace Bms.System.Infrastructure.Security;

/// <summary>
/// 密码哈希服务实现（使用BCrypt）
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    // BCrypt工作因子，越高越安全但越慢，推荐10-12
    private const int WorkFactor = 11;

    /// <summary>
    /// 哈希密码
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("密码不能为空", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // BCrypt验证失败时返回false
            return false;
        }
    }
}
