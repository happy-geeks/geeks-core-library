using System;
using System.Security.Cryptography;
using System.Text;

namespace GeeksCoreLibrary.Core.Helpers;

public static class SecurityHelpers
{
    /// <summary>
    /// Generates a random password of a certain length.
    /// </summary>
    /// <param name="length">The length of the password. The default is 16.</param>
    /// <returns>A string of the specified length with random characters.</returns>
    public static string GenerateRandomPassword(int length = 16)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        var result = new StringBuilder();
        using (var rng = RandomNumberGenerator.Create())
        {
            var uintBuffer = new byte[sizeof(uint)];

            while (length-- > 0)
            {
                rng.GetBytes(uintBuffer);
                var num = BitConverter.ToUInt32(uintBuffer, 0);
                result.Append(valid[(int)(num % (uint)valid.Length)]);
            }
        }

        return result.ToString();
    }
}