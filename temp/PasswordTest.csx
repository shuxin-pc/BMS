
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

// 生成正确的密码哈希
string password = "Admin@123";
int workFactor = 11;

string hash = BCrypt.HashPassword(password, workFactor);
Console.WriteLine($"密码: {password}");
Console.WriteLine($"哈希: {hash}");

// 验证一下
bool isValid = BCrypt.Verify(password, hash);
Console.WriteLine($"验证结果: {isValid}");
