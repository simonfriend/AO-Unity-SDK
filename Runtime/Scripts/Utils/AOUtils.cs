using System;
using System.Text.RegularExpressions;

namespace Permaverse.AO
{
    public static class AOUtils
    {
        public static string ShortenProcessID(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 10)
            {
                return input;
            }

            string start = input.Substring(0, 5);

            string end = input.Substring(input.Length - 5, 5);

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