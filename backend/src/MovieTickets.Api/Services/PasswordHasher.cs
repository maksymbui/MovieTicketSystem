using System.Security.Cryptography;
using System.Text;

namespace MovieTickets.Api.Services;

public static class PasswordHasher
{
    // PBKDF2 with HMACSHA256
    public static string Hash(string password, int iterations = 100_000, int saltSize = 16, int keySize = 32)
    {
        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var dk = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keySize);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(dk)}";
    }

    public static bool Verify(string password, string hash)
    {
        try
        {
            var parts = hash.Split('.');
            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);
            var dk = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, key.Length);
            return CryptographicOperations.FixedTimeEquals(dk, key);
        }
        catch
        {
            return false;
        }
    }
}
