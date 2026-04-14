using System;
using System.Security.Cryptography;
using System.Text;

namespace InternalDebugMenu
{
    public static class DebugCodeUtility
    {
        public static string ComputeSha256(string value)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var hash = sha256.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        public static bool SecureEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);

            if (leftBytes.Length != rightBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
