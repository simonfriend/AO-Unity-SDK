# Getting Started with Permaverse AO Unity SDK

This guide will walk you through setting up and using the Permaverse AO Unity SDK in your project.

---

## Installation

Follow the [installation instructions in the README](README.md#installation) to add the package to your Unity project.

---

## Setting Up AOConnectManager

1. Drag the **AOConnectManager** prefab into your Unity scene.
2. Configure the settings in the Inspector, such as your AO credentials.
3. Call methods from `AOConnectManager.main` in your scripts to interact with the AO infrastructure.

Example:
```csharp
using Permaverse.AO;
using UnityEngine;

public class MyAOTest : MonoBehaviour
{
    void Start()
    {
        AOConnectManager.main.OnCurrentAddressChange += OnCurrentAddressChanged;
        Debug.Log("Connected to AO!");
    }
    
    private void OnCurrentAddressChanged()
    {
        string currentAddress = AOConnectManager.main.CurrentAddress;
        if (string.IsNullOrEmpty(currentAddress))
        {
            Debug.LogError("Address is null or empty!!");
            return;
        }

        Debug.Log($"Current address changed to: {currentAddress}");
    }
}
```

---

## Building for WebGL

### **1. Copy the WebGL Template**
Unity does not automatically detect WebGL templates from packages, so you need to manually copy it:

1. Copy the **WebGLTemplates** folder from:
   ```
   Packages/AO Permaverse SDK/WebGLTemplates/
   ```
   to:
   ```
   Assets/WebGLTemplates/
   ```

2. Switch to WebGL in **File → Build Settings**.
3. In **Project Settings → Player → WebGL → Resolution and Presentation**, select the **AOTemplate** WebGL Template.

### **2. Set Up WebGL Player Settings**

Before building for WebGL, update these **Project Settings** to ensure compatibility:

#### In **Project Settings → Player → WebGL → Other Settings**
- **Color Space:** Change from **Linear** to **Gamma**.
- **Texture Compression Format:** Set to **ASTC**.

#### In **Project Settings → Player → WebGL → Publishing Settings**
- **Compression Format:** **Disabled** (to avoid compatibility issues).
- **Decompression Fallback:** **Enabled (true)** (for browser support).
- **Target WebAssembly 2023:** **Enabled (true)**.

### **3. Build the Unity WebGL Project**
1. Open **File → Build Settings** in Unity.
2. Click **Build** and choose an output folder (e.g., `WebGLBuild/`).

### **4. Run the Build Script**
After Unity has finished building:

1. Open the **WebGLBuild/build-tools/** folder.
2. **Double-click** the correct file to run it:
   - **On macOS/Linux**: `setup-mac-linux.command`
   - **On Windows**: `setup-windows.bat`

This will **install dependencies, build the JavaScript file, and start a local server**.

### **5. Test the WebGL Build**
Once complete, open:
```
http://localhost:8000
```
in your browser to test the WebGL build.

---

## Using MessageHandler

1. Use `MessageHandler` classes to send messages or perform dry runs.
2. Example usage:
```csharp
MessageHandler messageHandler;

private void SendMessage(string target, string data, string actionTag)
{
    List<Tag> tags = new List<Tag>();
    Tag actionTag = new Tag("Action", actionTag);
    tags.Add(actionTag);

    //To send a dryrun
    messageHandler.SendRequest(target, tags, OnDryrunResult, data, MessageHandler.NetworkMethod.Dryrun);
    
    //To send an on-chain message
    messageHandler.SendRequest(target, tags, OnMessageResult, data, MessageHandler.NetworkMethod.Message);
}

public void OnDryrunResult(bool result, NodeCU nodeCU)
{
    Debug.Log($"Dryrun Result: {result}");
    Debug.Log($"Node: {nodeCU.ToString()}");        
}

public void OnMessageResult(bool result, NodeCU nodeCU)
{
    Debug.Log($"Message Result: {result}");
    Debug.Log($"Node: {nodeCU.ToString()}");        
}
```

---

## Importing GLTF Assets with GLTFAssetManager

1. Place `GLTFAssetManager` prefab in scene in order to load `.gltf` and `.glb` models from Arweave.
2. Example usage:
```csharp

public async void DownloadAsset(string assetID, GameObject parentObject)
{
    GameObject asset = await GLTFAssetManager.main.GetAsset(assetID, parentObject);
}
```

---

## Further Help

If you encounter issues or need further assistance, please visit the [Permaverse AO Unity SDK GitHub Issues page](https://github.com/simonfriend/AO-Unity-SDK/issues).

---
