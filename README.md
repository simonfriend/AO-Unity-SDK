# Permaverse AO Unity SDK

**Permaverse AO Unity SDK** is a comprehensive Unity package for building decentralized applications on Arweave and AO infrastructure. Create fully on-chain Unity games and applications with multi-wallet support, high-performance messaging, and seamless WebGL deployment.

---

## ✨ Key Features

- **🔐 Multi-Wallet Support** 
  - Arweave wallets via **Wander Connect** (no extension required)
  - EVM wallets with **embedded session keys** (verified messages without popups)
  - Connect multiple wallets simultaneously
- **⚡ Dual Networking Modes**
  - **HyperBEAM** - Fast message processing with local/remote nodes
  - **Legacy AO** - Traditional messages, dryruns, and results
- **📊 GraphQL Utilities** - Query Arweave network with built-in helpers
- **📬 Advanced Messaging** - Queue-based, paginated, and periodic patterns
- **🎮 Editor Testing** - Test without WebGL builds using Node.js
- **♻️ Zero-Allocation Async** - UniTask-powered for optimal performance
- **🛍️ Bazar Marketplace** - Integrated decentralized marketplace
- **🎨 GLTF Assets** - Load 3D models directly from Arweave

---

## 📋 Requirements

- Unity **6** or later (6000.x.x)
- **UniTask** package (required)
- Node.js **v16+** (for Editor testing)
- Basic [AO](http://cookbook_ao.ar.io/) knowledge

---

## 📦 Installation

### Step 1: Install UniTask (Required)
```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

### Step 2: Install AO SDK
```
https://github.com/simonfriend/AO-Unity-SDK.git
```

Or add both to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.permaverse.ao-sdk": "https://github.com/simonfriend/AO-Unity-SDK.git"
  }
}
```

---

## 🚀 Quick Start

### 1. Add AOConnectManager
Drag the **AOConnectManager** prefab from `Packages/Permaverse AO SDK/Runtime/Prefabs/` into your scene.

### 2. Basic Usage
```csharp
using Permaverse.AO;
using UnityEngine;

public class AOExample : MonoBehaviour
{
    public MessageHandler messageHandler;
    
    void Start()
    {
        // Listen for wallet connections
        AOConnectManager.main.OnWalletConnected += OnWalletConnected;
    }
    
    void OnWalletConnected(WalletType walletType)
    {
        Debug.Log($"Connected: {walletType}");
        SendMessage();
    }
    
    async void SendMessage()
    {
        var tags = new List<Tag> { new Tag("Action", "GetInfo") };
        
        // Use HyperBEAM (fast)
        var (success, result) = await messageHandler.SendRequestAsync(
            "YOUR_PROCESS_ID", tags, method: MessageHandler.NetworkMethod.HyperBeamMessage
        );
        
        // Or use Legacy AO
        var (success2, result2) = await messageHandler.SendRequestAsync(
            "YOUR_PROCESS_ID", tags, method: MessageHandler.NetworkMethod.Message
        );
        
        // Or use Dryrun (testing)
        var (success3, result3) = await messageHandler.SendRequestAsync(
            "YOUR_PROCESS_ID", tags, method: MessageHandler.NetworkMethod.Dryrun
        );
    }
}
```

### 3. WebGL Deployment
1. Copy `Packages/Permaverse AO SDK/WebGLTemplates/` to `Assets/WebGLTemplates/`
2. Set Player Settings: **Gamma** color, **Disabled** compression
3. Build and run setup script in `WebGLBuild/build-tools/`

---

## 📚 Documentation

- **[Getting Started Guide](GettingStarted.md)** - Complete setup and usage guide
- **[AO Cookbook](http://cookbook_ao.ar.io/)** - Learn AO concepts
- **[HyperBEAM Documentation](https://hyperbeam.arweave.net/build/introduction/what-is-hyperbeam.html)** - Fast messaging system
- **[GitHub Issues](https://github.com/simonfriend/AO-Unity-SDK/issues)** - Support

---

## 🌐 Networking Capabilities

| Handler | Purpose | Supported Modes |
|---------|---------|-----------------|
| **MessageHandler** | Core networking | Legacy AO messages, HyperBEAM, Dryruns |
| **EnqueueMessageHandler** | Rate-limited queue | All MessageHandler modes |
| **PaginatedMessageHandler** | Scroll-based pagination | All MessageHandler modes |
| **HyperBeamPathHandler** | Fast HyperBEAM queries | HyperBEAM only |
| **GraphQLHandler** | Arweave network queries | GraphQL endpoints |

### Network Modes Explained

- **Legacy AO** (`NetworkMethod.Message`) - Traditional AO messages with results
- **HyperBEAM** (`NetworkMethod.HyperBeamMessage`) - Fast local/remote processing
- **Dryrun** (`NetworkMethod.Dryrun`) - Test messages without on-chain commitment

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file.