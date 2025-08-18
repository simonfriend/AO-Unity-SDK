using System;
using System.Security.Cryptography;

namespace Permaverse.AO
{
    public static class Base64Utils
    {
        // Converts Base64Url string to buffer
        public static byte[] B64UrlToBuffer(string base64Url)
        {
            string base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        // Converts buffer to Base64Url string
        public static string BufferToB64Url(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);
            return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
        }

        // Helper method to hash data (SHA-256)
        public static byte[] Hash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        // Converts the "n" field to an address by hashing
        public static string OwnerToAddress(string n)
        {
            byte[] nBuffer = B64UrlToBuffer(n);
            byte[] hash = Hash(nBuffer);
            return BufferToB64Url(hash);
        }
    }
}
