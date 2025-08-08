using System.Security.Cryptography;

namespace Fake.Helpers;

public class MD5Helper
{
    public static string GenerateSalt()
    {
        var buf = new byte[16];
        RandomNumberGenerator.Create().GetBytes(buf);

        return buf.ToBase64();
    }

    public static string GeneratePassword(string input, string salt)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentNullException(nameof(input), "Input cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(salt))
        {
            throw new ArgumentNullException(nameof(salt), "Salt cannot be null or empty.");
        }

        using (var md5 = MD5.Create())
        {
            var saltedInput = $"{input}{salt}";
            var hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(saltedInput));
            return hashBytes.ToHex();
        }
    }
}