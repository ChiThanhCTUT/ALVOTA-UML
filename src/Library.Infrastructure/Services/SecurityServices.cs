using System.Security.Cryptography;
using Library.Application.Common.Interfaces;

namespace Library.Infrastructure.Services;

// AUTH-02: Mật khẩu băm bằng PBKDF2 với salt
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, Algorithm, KeySize);

        var salt = Convert.ToBase64String(saltBytes);
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var expectedHashBytes = Convert.FromBase64String(hash);
        var actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, Algorithm, KeySize);

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }
}

public sealed class CurrentUserService : ICurrentUserService
{
    public string? UserId { get; private set; } = "Admin";
    public string? Username { get; private set; } = "admin";
    public string? Role { get; private set; } = "Admin";
    public bool IsAuthenticated => !string.IsNullOrEmpty(Username);

    public void SetUser(string userId, string username, string role)
    {
        UserId = userId;
        Username = username;
        Role = role;
    }

    public void Clear()
    {
        UserId = null;
        Username = null;
        Role = null;
    }
}
