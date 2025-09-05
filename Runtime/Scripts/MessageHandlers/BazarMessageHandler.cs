using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SimpleJSON;
using UnityEngine;

namespace Permaverse.AO
{
    [Serializable]
    public class AtomicAsset
    {
        public string Id;
        public int Quantity;
    }

    [Serializable]
    public class Profile
    {
        public string Id;
        public string Version;
        public string ProfileImage;
        public string UserName;
        public string CoverImage;
        public string Description;
        public long DateUpdated;
        public string DisplayName;
        public long DateCreated;
    }

    [Serializable]
    public class BazarProfileData
    {
        public List<AtomicAsset> Assets = new List<AtomicAsset>();
        public string Owner;
        public List<string> Collections = new List<string>();
        public Profile Profile;

        // Method to parse JSON string into this structure
        public static BazarProfileData ParseFromJSON(string jsonString)
        {
            var node = JSON.Parse(jsonString);
            var bazarProfileData = new BazarProfileData();

            // Parse Assets
            foreach (JSONNode assetNode in node["Assets"].AsArray)
            {
                var asset = new AtomicAsset
                {
                    Quantity = assetNode["Quantity"].AsInt,
                    Id = assetNode["Id"]
                };
                bazarProfileData.Assets.Add(asset);
            }

            // Parse Owner
            bazarProfileData.Owner = node["Owner"];

            // Parse Collections
            foreach (JSONNode collection in node["Collections"].AsArray)
            {
                bazarProfileData.Collections.Add(collection);
            }

            // Parse Profile
            var profileNode = node["Profile"];
            bazarProfileData.Profile = new Profile
            {
                Version = profileNode["Version"],
                ProfileImage = profileNode["ProfileImage"],
                UserName = profileNode["UserName"],
                CoverImage = profileNode["CoverImage"],
                Description = profileNode["Description"],
                DateUpdated = profileNode["DateUpdated"].AsLong,
                DisplayName = profileNode["DisplayName"],
                DateCreated = profileNode["DateCreated"].AsLong
            };

            return bazarProfileData;
        }
    }

    public class BazarMessageHandler : MessageHandler
    {
        [Header("Bazar Message Handler")]
        public Action<BazarProfileData> OnBazarDataReceived;
        public Action<string, string, int> OnAssetDataReceived;
        private string pid = "SNy4m-DrqxWl01YqGM4sxI8qCni-58re8uuJLvZPypY";

        public void GetBazarInfo(string address)
        {
            List<Tag> tags = new List<Tag>
        {
            new("Action", "Read-Profiles"),
        };

            string data = "{\"Addresses\" : [\"" + address + "\"]}";

            if (showLogs)
            {
                Debug.LogError("Data: " + data);
            }

            Action<bool, NodeCU> callback = (bool result, NodeCU response) =>
            {
                if (!result || string.IsNullOrEmpty(response.Messages[0].Data))
                {
                    if (showLogs)
                    {
                        Debug.LogError($"[{gameObject.name}] no bazar profile");
                    }

                    OnBazarDataReceived?.Invoke(null);

                    return;
                }

                if (showLogs)
                {
                    Debug.LogError(response.Messages[0].Data);
                }

                JSONArray node = JSON.Parse(response.Messages[0].Data).AsArray;

                if (node.Count > 0 && node[0].HasKey("ProfileId"))
                {
                    string callerAddress = node[0]["CallerAddress"];
                    string profileId = node[0]["ProfileId"];

                    if (callerAddress == address)
                    {
                        List<Tag> tags = new List<Tag>
                        {
                        new("Action", "Info"),
                        };

                        Action<bool, NodeCU> callback = (bool result, NodeCU response) =>
                        {
                            if (!result || string.IsNullOrEmpty(response.Messages[0].Data))
                            {
                                Debug.LogError($"[{gameObject.name}] problem retrieving data from profile {profileId}");
                                OnBazarDataReceived?.Invoke(null);
                                return;
                            }

                            if (showLogs)
                            {
                                Debug.LogError(response.Messages[0].Data);
                            }

                            BazarProfileData bazarProfileData = BazarProfileData.ParseFromJSON(response.Messages[0].Data);
                            bazarProfileData.Profile.Id = profileId;
                            OnBazarDataReceived?.Invoke(bazarProfileData);
                        };

                        SendRequestAsync(profileId, tags, useMainWallet: true, callback: callback).Forget();
                        return;
                    }
                }

                OnBazarDataReceived?.Invoke(null);
            };

                        SendRequestAsync(pid, tags, data: data, useMainWallet: true, callback: callback).Forget();
        }

        public void GetAssetInfo(string assetID, string bazarID = null)
        {
            List<Tag> tags = new List<Tag>
        {
            new("Action", "Balance"),
        };

            string currentBazarID = AOConnectManager.main.CurrentBazarID;
            string data = "{\"Target\" : [\"" + currentBazarID + "\"]}";

            Action<bool, NodeCU> callback = (bool result, NodeCU response) =>
            {
                if (!result || string.IsNullOrEmpty(response.Messages[0].Data))
                {
                    Debug.LogError($"[{gameObject.name}] no asset {assetID} in profile {AOConnectManager.main.CurrentBazarID}");
                    OnAssetDataReceived?.Invoke(currentBazarID, assetID, 0);
                    return;
                }

                if (showLogs)
                {
                    Debug.LogError(response.Messages[0].Data);
                }

                int count = int.Parse(response.Messages[0].Data);

                OnAssetDataReceived?.Invoke(currentBazarID, assetID, count);
            };

            SendRequestAsync(assetID, tags, data: data, useMainWallet: true, callback: callback).Forget();
        }
    }
}