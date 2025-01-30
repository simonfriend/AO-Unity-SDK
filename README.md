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

1. Open your Unity project’s `Packages/manifest.json` file in a text editor.
2. Add this line to the `"dependencies"` section (adjust the Git URL/tag to your actual repository path and version):

   ```json
   {
     "dependencies": {
       "com.permaverse.ao-sdk": "https://github.com/simonfriend/AO-Unity-SDK.git#v0.1.0"
     }
   }
   ```

3. Save `manifest.json` and return to Unity. Unity will download the AO SDK and any required dependencies automatically.

**Alternative**: In Unity, go to **Window → Package Manager** → **+** → **“Add package from git URL…”** → Paste:
```
https://github.com/simonfriend/AO-Unity-SDK.git#v0.1.0
```
and click **Add**.

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
