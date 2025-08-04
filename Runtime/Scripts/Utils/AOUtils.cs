using System;
using System.Text.RegularExpressions;

namespace Permaverse.AO
{
    public static class AOUtils
    {
        public static string ShortenString(string input, int maxLength = 10)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }

            if (maxLength < 7) // Need at least 7 characters for "a...b" format
            {
                return input.Substring(0, Math.Min(maxLength, input.Length));
            }

            int sideLength = (maxLength - 3) / 2; // 3 for "..."
            string start = input.Substring(0, sideLength);
            string end = input.Substring(input.Length - sideLength, sideLength);

            return start + "..." + end;
        }

        public static bool IsEVMWallet(string walletAddress)
        {
            // Check if the wallet address starts with '0x'
            if (!walletAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check if the wallet address is 42 characters long
            if (walletAddress.Length != 42)
            {
                return false;
            }

            // Check if the wallet address contains only valid hexadecimal characters
            string hexPattern = @"\A\b[0-9a-fA-F]+\b\Z";
            string addressWithoutPrefix = walletAddress.Substring(2); // Remove '0x'
            return Regex.IsMatch(addressWithoutPrefix, hexPattern);
        }
    }
}