using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Linq;
using Permaverse.AO;

namespace Permaverse.AO.Editor
{
    /// <summary>
    /// Message sending mode for AO communication
    /// </summary>
    public enum MessageMode
    {
        HyperBeam,
        Legacy
    }

    /// <summary>
    /// Unity Editor utility for testing HyperBEAM messages without building to WebGL
    /// Uses Node.js script to send messages via aoconnect with real wallet signing
    /// </summary>
    public class HyperBeamEditorTester : EditorWindow
    {
        [Header("Configuration")]
        [SerializeField] private string processId = "t9qaxM7bEyxrzJ2PG52qyvP4h3ub6DG775M6XbSAYsY";
        [SerializeField] private string hyperBeamUrl = "http://localhost:8734";
        [SerializeField] private bool verbose = true;
        [SerializeField] private MessageMode messageMode = MessageMode.HyperBeam;

        // [Header("Debug")]
        // [SerializeField]
        private bool verboseLogs = false;
        
        [Header("Message")]
        [SerializeField] private string messageData = "";
        [SerializeField] private List<MessageTag> tags = new List<MessageTag>();
        
        [Header("Output")]
        [SerializeField] private bool prettyPrint = true;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private Vector2 mainScrollPosition; // Main scroll for entire window
        [SerializeField] private string lastResponse = "";
        [SerializeField] private bool showResponse = true;

        [System.Serializable]
        public class MessageTag
        {
            public string name = "";
            public string value = "";
            public bool enabled = true;
        }

        // Common tag presets
        private static readonly Dictionary<string, MessageTag[]> TagPresets = new Dictionary<string, MessageTag[]>
        {
            ["GetUserInfo"] = new[] { 
                new MessageTag { name = "Action", value = "GetUserInfo", enabled = true },
            },
            ["GetLeaderboard"] = new[] { 
                new MessageTag { name = "Action", value = "GetLeaderboard", enabled = true },
                new MessageTag { name = "Page", value = "1", enabled = true },
                new MessageTag { name = "PageSize", value = "10", enabled = true },
            },
            ["GetMatchInfo"] = new[] { new MessageTag { name = "Action", value = "GetMatchInfo", enabled = true } },
        };

        [MenuItem("Tools/AO/AO Message Editor Tester")]
        public static void ShowWindow()
        {
            GetWindow<HyperBeamEditorTester>("AO Message Tester");
        }

        private void OnEnable()
        {
            // Initialize with GetUserInfo preset if no tags exist
            if (tags.Count == 0)
            {
                LoadPreset("GetUserInfo");
            }
        }

        /// <summary>
        /// Find AOConnectManager in all loaded scenes, not just the active one
        /// </summary>
        private AOConnectManager FindAOConnectManager()
        {
            // Search in all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var rootObject in rootObjects)
                    {
                        var aoManager = rootObject.GetComponentInChildren<AOConnectManager>();
                        if (aoManager != null)
                        {
                            return aoManager;
                        }
                    }
                }
            }
            return null;
        }

        private void OnGUI()
        {
            // Main scroll view for entire window content
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            EditorGUILayout.LabelField("AO Message Editor Tester", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Integration info box - Find AOConnectManager in all loaded scenes
            var aoManager = FindAOConnectManager();
            if (aoManager != null)
            {
                if (!string.IsNullOrEmpty(aoManager.editorWalletPath) && File.Exists(aoManager.editorWalletPath))
                {
                    EditorGUILayout.HelpBox(
                        $"✅ Wallet configured in AOConnectManager: {Path.GetFileName(aoManager.editorWalletPath)}\n" +
                        "Ready for editor testing with real wallet signing.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "⚠️ No wallet configured in AOConnectManager.\n" +
                        "Select the AOConnectManager in your scene and set the Editor Wallet Path to enable testing.",
                        MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "⚠️ No AOConnectManager found in any loaded scene.\n" +
                    "Add an AOConnectManager to your scene and configure the wallet path for editor testing.",
                    MessageType.Warning);
            }
            EditorGUILayout.Space();

            if (aoManager != null)
            {
                EditorGUILayout.LabelField("SDK Integration", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"AOConnectManager found - HyperBEAM URL: {aoManager.hyperBeamUrl}");
                if (GUILayout.Button("Use SDK Settings", GUILayout.Width(120)))
                {
                    hyperBeamUrl = aoManager.hyperBeamUrl;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            // Configuration section
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            processId = EditorGUILayout.TextField("Process ID", processId);
            hyperBeamUrl = EditorGUILayout.TextField("HyperBEAM URL", hyperBeamUrl);
            verbose = EditorGUILayout.Toggle("Verbose Output", verbose);
            
            // Message mode selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Message Mode", GUILayout.Width(100));
            messageMode = (MessageMode)EditorGUILayout.EnumPopup(messageMode, GUILayout.Width(100));
            EditorGUILayout.LabelField(messageMode == MessageMode.HyperBeam ? 
                "Uses HyperBEAM for faster message processing" : 
                "Uses legacy AO connect for traditional message flow", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            // Debug section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            verboseLogs = EditorGUILayout.Toggle("Verbose Debug Logs", verboseLogs);
            
            // Setup instructions
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Setup Dependencies", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Run the setup script to install Node.js dependencies:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("cd Packages/com.permaverse.ao-sdk/EditorConnect", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("./setup.sh", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Or see the README.md for detailed setup instructions.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Presets section
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var preset in TagPresets.Keys)
            {
                if (GUILayout.Button(preset, GUILayout.Width(120)))
                {
                    LoadPreset(preset);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Message section
            EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Data:");
            messageData = EditorGUILayout.TextArea(messageData, GUILayout.Height(60));
            EditorGUILayout.Space();

            // Tags section
            EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
            for (int i = 0; i < tags.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                tags[i].enabled = EditorGUILayout.Toggle(tags[i].enabled, GUILayout.Width(20));
                tags[i].name = EditorGUILayout.TextField(tags[i].name);
                tags[i].value = EditorGUILayout.TextField(tags[i].value);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    tags.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Tag"))
            {
                tags.Add(new MessageTag { name = "NewTag", value = "", enabled = true });
            }
            if (GUILayout.Button("Clear All"))
            {
                tags.Clear();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Send button
            // EditorGUI.BeginDisabledGroup(Application.isPlaying);
            string buttonText = messageMode == MessageMode.HyperBeam ? "Send HyperBEAM Message" : "Send Legacy AO Message";
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                if (verboseLogs) UnityEngine.Debug.Log($"[AO Tester] Button clicked - starting {messageMode} message send...");
                _ = SendMessage();
            }
            // if (Application.isPlaying)
            // {
            //     EditorGUILayout.HelpBox("Cannot send messages while playing. Stop the editor first.", MessageType.Warning);
            // }
            // EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();

            // Response section
            if (showResponse && !string.IsNullOrEmpty(lastResponse))
            {
                EditorGUILayout.LabelField("Last Response", EditorStyles.boldLabel);
                prettyPrint = EditorGUILayout.Toggle("Pretty Print", prettyPrint);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                string displayText = prettyPrint ? FormatJson(lastResponse) : lastResponse;
                EditorGUILayout.SelectableLabel(displayText, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Copy Response"))
                {
                    EditorGUIUtility.systemCopyBuffer = lastResponse;
                }
            }
            
            // End main scroll view
            EditorGUILayout.EndScrollView();
        }

        private void LoadPreset(string presetName)
        {
            if (TagPresets.TryGetValue(presetName, out var presetTags))
            {
                tags.Clear();
                foreach (var tag in presetTags)
                {
                    var newTag = new MessageTag { name = tag.name, value = tag.value, enabled = tag.enabled };
                    tags.Add(newTag);
                }
                Repaint();
            }
        }

        private async Task SendMessage()
        {
            if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] SendMessage() method started");
            
            try
            {
                if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] Getting wallet path...");
                // Try to get wallet path from AOConnectManager first
                string walletPath = GetWalletPath();
                if (string.IsNullOrEmpty(walletPath))
                {
                    UnityEngine.Debug.LogError("[HyperBEAM Tester] No wallet path found");
                    EditorUtility.DisplayDialog("Error", 
                        "No wallet keyfile configured.\n\n" +
                        "Please set up the wallet path in the AOConnectManager component in your scene, " +
                        "or ensure the script can find the default wallet file.", "OK");
                    return;
                }
                if (verboseLogs) UnityEngine.Debug.Log($"[HyperBEAM Tester] Wallet path found: {Path.GetFileName(walletPath)}");

                if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] Getting script path...");
                string scriptPath = GetScriptPath();
                if (!File.Exists(scriptPath))
                {
                    UnityEngine.Debug.LogError($"[HyperBEAM Tester] Script not found at: {scriptPath}");
                    EditorUtility.DisplayDialog("Error", $"HyperBEAM tester script not found at:\n{scriptPath}\n\nMake sure the script exists and npm packages are installed.", "OK");
                    return;
                }
                if (verboseLogs) UnityEngine.Debug.Log($"[HyperBEAM Tester] Script path found: {scriptPath}");

                if (verboseLogs) UnityEngine.Debug.Log("[AO Tester] Building arguments...");
                var arguments = new List<string> { scriptPath };
                
                // Add configuration
                arguments.Add("--process-id");
                arguments.Add(processId);
                arguments.Add("--wallet");
                arguments.Add(walletPath);
                arguments.Add("--output");
                arguments.Add("unity");
                arguments.Add("--mode");
                arguments.Add(messageMode == MessageMode.HyperBeam ? "hyperbeam" : "legacy");
                
                // Add HyperBEAM URL only for HyperBEAM mode
                if (messageMode == MessageMode.HyperBeam)
                {
                    arguments.Add("--hyperbeam-url");
                    arguments.Add(hyperBeamUrl);
                }
                
                if (verbose)
                {
                    arguments.Add("--verbose");
                }

                // Add data if provided
                if (!string.IsNullOrEmpty(messageData))
                {
                    arguments.Add("--data");
                    arguments.Add(messageData);
                }

                // Add enabled tags
                foreach (var tag in tags)
                {
                    if (tag.enabled && !string.IsNullOrEmpty(tag.name))
                    {
                        arguments.Add($"--tag-{tag.name}={tag.value}");
                    }
                }

                UnityEngine.Debug.Log($"[AO Tester] Sending {messageMode} message with {tags.FindAll(t => t.enabled).Count} tags using wallet: {walletPath}");

                // Execute Node.js script
                var result = await ExecuteNodeScript(arguments.ToArray());
                
                lastResponse = result;
                showResponse = true;
                
                // Parse response for success/error
                try
                {
                    UnityEngine.Debug.Log($"[HyperBEAM Tester] Raw response: {result}");
                    var json = JSON.Parse(result);
                    if (json["success"].AsBool)
                    {
                        UnityEngine.Debug.Log($"[AO Tester] {messageMode} message sent successfully!");
                        if (verbose && json["data"] != null)
                        {
                            UnityEngine.Debug.Log($"[HyperBEAM Tester] Response data: {json["data"]}");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[HyperBEAM Tester] Failed: {json["error"]}");
                    }
                }
                catch
                {
                    UnityEngine.Debug.Log($"[HyperBEAM Tester] Raw response: {result}");
                }

                Repaint();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[HyperBEAM Tester] Error: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to send message:\n{e.Message}", "OK");
            }
        }

        private string GetWalletPath()
        {
            if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] GetWalletPath() - checking AOConnectManager...");
            // First try to get from AOConnectManager
            var aoManager = FindAOConnectManager();
            if (aoManager != null && !string.IsNullOrEmpty(aoManager.editorWalletPath) && File.Exists(aoManager.editorWalletPath))
            {
                if (verboseLogs) UnityEngine.Debug.Log($"[HyperBEAM Tester] Found wallet from AOConnectManager: {Path.GetFileName(aoManager.editorWalletPath)}");
                return aoManager.editorWalletPath;
            }
            
            if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] AOConnectManager wallet not found, checking default paths...");
            // Fallback to default wallet locations
            string[] defaultPaths = {
                Path.Combine(Application.dataPath, "..", "Packages", "com.permaverse.ao-sdk", "wallet.json"),
                Path.Combine(Application.dataPath, "..", "Packages", "com.permaverse.ao-sdk", "arweave-keyfile-vRrw1H_bgr2gF_7dtrkVN6kjmtXkKAA4BwJH8JXE2ys.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "wallet.json")
            };
            
            foreach (string path in defaultPaths)
            {
                if (verboseLogs) UnityEngine.Debug.Log($"[HyperBEAM Tester] Checking default path: {path}");
                if (File.Exists(path))
                {
                    if (verboseLogs) UnityEngine.Debug.Log($"[HyperBEAM Tester] Found wallet at default path: {Path.GetFileName(path)}");
                    return path;
                }
            }
            
            if (verboseLogs) UnityEngine.Debug.Log("[HyperBEAM Tester] No wallet found in any default paths");
            return null;
        }

        private string GetScriptPath()
        {
            // Look for the script in the AO SDK EditorConnect directory
            string[] searchPaths = {
                Path.Combine(Application.dataPath, "..", "Packages", "com.permaverse.ao-sdk", "EditorConnect", "aoconnect-editor.js"),
                Path.Combine(Directory.GetCurrentDirectory(), "Packages", "com.permaverse.ao-sdk", "EditorConnect", "aoconnect-editor.js"),
                Path.Combine(Application.dataPath, "Packages", "com.permaverse.ao-sdk", "EditorConnect", "aoconnect-editor.js")
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return searchPaths[0]; // Return first path as fallback for error message
        }

        private async Task<string> ExecuteNodeScript(string[] arguments)
        {
            try
            {
                return await NodeJsUtils.ExecuteNodeScriptAsync(arguments);
            }
            catch (Exception e)
            {
                throw new Exception($"Node.js execution failed: {e.Message}");
            }
        }

        private string GetWalletAddressFromKeyfile()
        {
            try
            {
                string walletPath = GetWalletPath();
                if (string.IsNullOrEmpty(walletPath) || !File.Exists(walletPath))
                {
                    return null;
                }

                // Use WalletUtils to get the proper Arweave address
                string walletAddress = WalletUtils.GetWalletIDFromPath(walletPath);
                return walletAddress;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Failed to extract wallet address: {e.Message}");
            }
            
            return "[WALLET_FROM_KEYFILE]"; // Fallback placeholder
        }

        private string FormatJson(string json)
        {
            try
            {
                var parsed = JSON.Parse(json);
                return parsed.ToString(2); // Pretty print with 2-space indentation
            }
            catch
            {
                return json; // Return as-is if parsing fails
            }
        }
    }
}
