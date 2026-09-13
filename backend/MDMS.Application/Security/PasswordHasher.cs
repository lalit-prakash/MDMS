using System.Security.Cryptography;

namespace MDMS.Application.Security;

/// <summary>
/// PBKDF2-HMACSHA256 password hashing — the same algorithm ASP.NET Core Identity's default
/// hasher uses, implemented directly against the BCL (<see cref="Rfc2898DeriveBytes"/>) rather
/// than pulling in the Identity package for one utility class. Stored format:
/// "{iterations}.{saltBase64}.{subkeyBase64}" — self-describing so the iteration count can be
/// raised later without invalidating already-hashed passwords.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit salt
    private const int SubkeySize = 32; // 256-bit derived key
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, SubkeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public static bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
            return false;

        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        byte[] salt, expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedSubkey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedSubkey.Length);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
