using System.Security.Cryptography;
using LogicFit.Application;

namespace LogicFit.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "lf-pbkdf2-sha256$v1$";
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string encodedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(encodedHash) || string.IsNullOrEmpty(password) || !encodedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var parts = encodedHash[Prefix.Length..].Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations < 100_000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
