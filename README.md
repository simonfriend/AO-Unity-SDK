# Permaverse AO Unity SDK

**Permaverse AO Unity SDK** is a Unity package that provides tooling and scripts to integrate and interact with AO infrastructure from Unity.

It includes:
- Scripts for AO session and message management.
- Support for WebGL (via a custom WebGL template and `.jslib` interop).

---

## Features

- **AOConnectManager**: Handles connecting, messaging, and other AO functionalities.
- **MessageHandler**: Sends AO messages, performs dry runs, and retrieves results directly in Unity.
- **GLTFAssetManager**: Imports custom `.gltf` and `.glb` assets from Arweave into your Unity app.
- **WebGL**: Includes a custom WebGL template and direct JavaScript interop.

---

## Requirements

- Unity **6** or later (e.g., `6000.x.x`).
- Basic knowledge of AO. You can learn more on [AO Cookbook](http://cookbook_ao.ar.io/).

---

## Installation

In Unity, go to **Window → Package Manager** → **+** → **“Add package from git URL…”** → Paste:
```
https://github.com/simonfriend/AO-Unity-SDK.git
```
and click **Add**.

**Alternative**: 

1. Open your Unity project’s `Packages/manifest.json` file in a text editor.
2. Add this line to the `"dependencies"` section (you can target a specific release by appending #tag at the end of the url):

   ```json
   {
     "dependencies": {
       "com.permaverse.ao-sdk": "https://github.com/simonfriend/AO-Unity-SDK.git"
     }
   }
   ```

3. Save `manifest.json` and return to Unity. Unity will download the AO SDK and any required dependencies automatically.

---

## Required Dependencies

### UniTask (Performance Optimization)

This SDK uses **UniTask** for high-performance async operations with zero allocations. You must install UniTask manually:

**Option 1: Package Manager UI (Recommended)**
1. Open **Window → Package Manager**
2. Click **+** → **"Add package from git URL…"**
3. Paste: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
4. Click **Add**

**Option 2: manifest.json**
1. Open `Packages/manifest.json`
2. Add to dependencies:
   ```json
   {
     "dependencies": {
       "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
       "com.permaverse.ao-sdk": "https://github.com/simonfriend/AO-Unity-SDK.git"
     }
   }
   ```

**Why UniTask?**
- 🚀 **Zero GC allocation** async operations
- ⚡ **Better performance** for real-time networking
- 🎮 **Unity-optimized** cancellation and lifecycle management

---

## Usage

After installation, you can:

1. **Use the AOConnectManager** in your scene.
2. Try the **BasicDemo** scene for a quick start. You can install sample content from the **Package Manager** UI. Look under **Permaverse AO Unity SDK** → **Samples** and click “Import” to bring a demo scene into your `Assets/` folder.
3. Use `MessageHandler` to start to use AO as a back-end to create fully on-chain dApps.

### Namespaces

All runtime scripts are under the namespace `Permaverse.AO`. Example usage:

```csharp
using Permaverse.AO;
using UnityEngine;

public class ExampleAOUsage : MonoBehaviour
{
    void Start()
    {
        Debug.Log(AOConnectManager.main.CurrentAddress);
    }
}
```

---

## Documentation

For detailed usage and troubleshooting, see:
- [Getting Started](GettingStarted.md)

---

## License

This project is open source under the [MIT License](LICENSE). Feel free to use, modify, and distribute as permitted.
