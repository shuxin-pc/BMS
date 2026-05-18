namespace Bms.System.Domain.Interfaces;

/// <summary>
/// 密码哈希服务接口
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// 哈希密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>哈希后的密码</returns>
    string HashPassword(string password);

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <param name="hashedPassword">哈希后的密码</param>
    /// <returns>是否匹配</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
