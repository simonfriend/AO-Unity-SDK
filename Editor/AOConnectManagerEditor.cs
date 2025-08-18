using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace Permaverse.AO.Editor
{
    [CustomEditor(typeof(AOConnectManager))]
    public class AOConnectManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AOConnectManager manager = (AOConnectManager)target;
            
            // Custom wallet file picker at the top
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Wallet Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wallet Keyfile", GUILayout.Width(100));
            
            // Display current path
            string displayPath = string.IsNullOrEmpty(manager.editorWalletPath) ? "No file selected" : manager.editorWalletPath;
            EditorGUILayout.LabelField(displayPath, EditorStyles.textField);
            
            // File picker button
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select Arweave Wallet Keyfile", 
                    string.IsNullOrEmpty(manager.editorWalletPath) ? Application.dataPath : Path.GetDirectoryName(manager.editorWalletPath),
                    "json"
                );
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    manager.editorWalletPath = selectedPath;
                    
                    // Auto-update the editorAddress field with the wallet address
                    try
                    {
                        string walletAddress = WalletUtils.GetWalletIDFromPath(selectedPath);
                        if (!string.IsNullOrEmpty(walletAddress))
                        {
                            manager.editorAddress = walletAddress;
                            Debug.Log($"Auto-updated editor address: {walletAddress}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Could not extract wallet address: {e.Message}");
                    }
                    
                    EditorUtility.SetDirty(manager);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Validation
            if (!string.IsNullOrEmpty(manager.editorWalletPath))
            {
                if (File.Exists(manager.editorWalletPath))
                {
                    try
                    {
                        string content = File.ReadAllText(manager.editorWalletPath);
                        var json = SimpleJSON.JSON.Parse(content);
                        if (json != null && json.HasKey("kty") && json["kty"] == "RSA")
                        {
                            EditorGUILayout.HelpBox("✅ Valid Arweave wallet keyfile detected", MessageType.Info);
                            
                            // // Show current editor address if set
                            // if (!string.IsNullOrEmpty(manager.editorAddress))
                            // {
                            //     EditorGUILayout.BeginHorizontal();
                            //     EditorGUILayout.LabelField("Editor Address:", GUILayout.Width(100));
                            //     EditorGUILayout.SelectableLabel(manager.editorAddress, EditorStyles.textField, GUILayout.Height(16));
                            //     EditorGUILayout.EndHorizontal();
                            // }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("⚠️ File exists but may not be a valid Arweave wallet keyfile", MessageType.Warning);
                        }
                    }
                    catch
                    {
                        EditorGUILayout.HelpBox("❌ Invalid JSON file", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("❌ File does not exist", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select an Arweave wallet keyfile to enable editor testing", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Quick test button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(manager.editorWalletPath) || !File.Exists(manager.editorWalletPath));
            if (GUILayout.Button("Open HyperBEAM Editor Tester"))
            {
                HyperBeamEditorTester.ShowWindow();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Component Settings", EditorStyles.boldLabel);
            
            // Draw the rest of the default inspector
            DrawDefaultInspector();
        }
    }
}
