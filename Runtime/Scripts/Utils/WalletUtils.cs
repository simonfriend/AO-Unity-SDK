using System.IO;
using UnityEngine;

namespace Permaverse.AO
{
    public static class WalletUtils
    {
        [System.Serializable]
        public class JWKInterface
        {
            public string kty;
            public string e;
            public string n;
            public string d;
            public string p;
            public string q;
            public string dp;
            public string dq;
            public string qi;
        }

        public static string GetWalletIDFromPath(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return GetWalletIDFromJWK(json);
            }
            catch
            {
                Debug.LogError("Failed to read wallet file.");
                return null;
            }
        }

        public static string GetWalletIDFromJWK(string jsonContent)
        {
            try
            {
                JWKInterface jwk = JsonUtility.FromJson<JWKInterface>(jsonContent);
                return Base64Utils.OwnerToAddress(jwk.n);
            }
            catch
            {
                Debug.LogError("Failed to read wallet file.");
                return null;
            }
        }
    }
}
