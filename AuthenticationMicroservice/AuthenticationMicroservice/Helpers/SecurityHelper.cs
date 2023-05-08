namespace AuthenticationMicroservice.Helpers;

using System.Security.Cryptography;
using System.Text;

public class SecurityHelper
{
    private const int SaltLength = 25;

    public static bool VerifyPassword(string enteredPassword, string passwordHash)
    {
        var salt = passwordHash[..25];
        var hashOfEnteredPassword = salt + Sha1Hash($"{salt}{enteredPassword}");
        return hashOfEnteredPassword == passwordHash;
    }

    public static string CreatePasswordHash(string newPassword, string? salt = null)
    {
        salt = string.IsNullOrWhiteSpace(salt) ? GenerateSalt() : salt[..SaltLength];
        return salt + Sha1Hash($"{salt}{newPassword}");
    }

    public static void EnsurePasswordComplexity(string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            throw new InvalidPasswordException("The passwords do not match.");
        if (!password.Equals(confirmPassword))
            throw new InvalidPasswordException("The passwords do not match.");
        if (password.Length < 8)
            throw new WeakPasswordException("The password must be at least 8 characters.");
        if (!password.Any(char.IsDigit))
            throw new WeakPasswordException("The password must contain at least 1 number.");
        if (!HasSpecialCharacters(password))
            throw new WeakPasswordException("The password must contain at least 1 special character.");
    }

    public static bool HasSpecialCharacters(string input)
    {
        return input.Any(ch => !char.IsLetterOrDigit(ch));
    }

    private static string Sha1Hash(string password)
    {
        return string.Join("", SHA1.HashData(Encoding.UTF8.GetBytes(password)).Select(x => x.ToString("x2")));
    }

    private static string GenerateSalt()
    {
        var randomMd5 = MD5.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        var randomMd5AsString = string.Join("", randomMd5);
        return randomMd5AsString[..SaltLength];
    }

    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string message) : base(message)
        {
        }
    }

    public class WeakPasswordException : Exception
    {
        public WeakPasswordException(string message) : base(message)
        {
        }
    }
}