using System.Collections.Generic;
using UnityEngine;
using GLTFast;
using System.Threading.Tasks;

namespace Permaverse.AO
{
    public class GLTFAssetManager : MonoBehaviour
    {
        public static GLTFAssetManager main { get; private set; }

        public Dictionary<string, GameObject> assetPrefabs = new Dictionary<string, GameObject>(); //TODO: Serialize it

        private string baseUrl = "https://arweave.net/";
        private Dictionary<string, GltfImport> assetGltf = new Dictionary<string, GltfImport>();

        private void Awake()
        {
            if (main == null)
            {
                main = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Method to get or download an asset
        public async Task<GameObject> GetAsset(string assetId, GameObject targetObject = null, ImportSettings settings = null)
        {
            // Check if the asset is already in the dictionary
            if (!assetGltf.ContainsKey(assetId))
            {
                // Asset not found, download and load it
                var gltf = new GltfImport();

                // Use default settings if none are provided
                //if (settings == null)
                //{
                //    settings = new ImportSettings
                //    {
                //        GenerateMipMaps = true,
                //        AnisotropicFilterLevel = 3,
                //        NodeNameMethod = NameImportMethod.OriginalUnique
                //    };
                //}

                var success = await gltf.Load(baseUrl + assetId, settings);

                if (success)
                {
                    // Store the GltfImport instance in the dictionary
                    assetGltf[assetId] = gltf;
                }
                else
                {
                    Debug.LogError($"Loading asset {assetId} failed!");
                    return null;
                }
            }

            // Instantiate the asset at the given transform or create a new GameObject if no transform is provided
            var gltfInstance = assetGltf[assetId];
            var gameObject = targetObject != null ? targetObject : new GameObject("glTF_Instance");
            await gltfInstance.InstantiateMainSceneAsync(gameObject.transform);

            return gameObject;
        }
    }
}