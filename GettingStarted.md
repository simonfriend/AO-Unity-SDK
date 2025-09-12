# Getting Started with Permaverse AO Unity SDK

This comprehensive guide covers everything from initial setup to advanced features of the Permaverse AO Unity SDK.

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Project Setup](#project-setup)
4. [Wallet Management](#wallet-management)
5. [Networking & Message Handlers](#networking--message-handlers)
6. [HyperBeam Integration](#hyperbeam-integration)
7. [GraphQL Utilities](#graphql-utilities)
8. [Unity Editor Testing](#unity-editor-testing)
9. [WebGL Deployment](#webgl-deployment)
10. [Advanced Features](#advanced-features)
11. [Troubleshooting](#troubleshooting)

---

## Wallet Management

### Multi-Wallet Architecture

The SDK supports multiple wallet connection methods:

#### Arweave Wallets (via Wander Connect)
- **No browser extension required** - Works through Wander Connect integration
- Supports both native and web-based Arweave wallets
- Seamless connection flow without popups

#### EVM Wallets (with Session Keys)
- **Embedded session keys** - Sign messages without MetaMask popups
- 7-day session expiration with auto-renewal
- Stored securely in browser localStorage
- Fallback to main wallet signing when needed

```csharp
public class WalletManager : MonoBehaviour
{
    private AOConnectManager manager => AOConnectManager.main;
    
    public void ConnectWallets()
    {
        // Connect Arweave wallet via Wander Connect (no extension needed)
        manager.ConnectWallet(WalletType.Arweave);
        
        // Connect EVM wallet with session keys (no popups for messages)
        manager.ConnectWallet(WalletType.EVM);
    }
    
    public void CheckSessionKey()
    {
        var evmInfo = manager.GetSecondaryWalletInfo(WalletType.EVM);
        if (evmInfo?.sessionKeyInfo != null)
        {
            Debug.Log($"Session key active: {evmInfo.sessionKeyInfo.address}");
            Debug.Log($"Expires: {evmInfo.sessionKeyInfo.expiryDate}");
            Debug.Log($"Main wallet: {evmInfo.sessionKeyInfo.mainWallet}");
        }
    }
}
```

---

## Networking & Message Handlers

### MessageHandler - Core Networking

Supports three network modes for different use cases:

```csharp
public class NetworkingExample : MonoBehaviour
{
    public MessageHandler handler;
    
    async void DemonstrateNetworkModes()
    {
        var tags = new List<Tag> 
        { 
            new Tag("Action", "GetData") 
        };
        
        // 1. Legacy AO Mode - Traditional AO messages with results
        var (success1, result1) = await handler.SendRequestAsync(
            pid: "process_id",
            tags: tags,
            method: MessageHandler.NetworkMethod.Message
        );
        
        // 2. HyperBEAM Mode - Fast processing via HyperBEAM
        var (success2, result2) = await handler.SendRequestAsync(
            pid: "process_id",
            tags: tags,
            method: MessageHandler.NetworkMethod.HyperBeamMessage
        );
        
        // 3. Dryrun Mode - Test without on-chain commitment
        var (success3, result3) = await handler.SendRequestAsync(
            pid: "process_id",
            tags: tags,
            method: MessageHandler.NetworkMethod.Dryrun
        );
    }
}
```

---

## GraphQL Utilities

The SDK includes powerful GraphQL utilities for querying the Arweave network:

### GraphQLHandler Features

```csharp
public class GraphQLExample : MonoBehaviour
{
    public GraphQLHandler graphqlHandler;
    
    async void DemonstrateGraphQL()
    {
        // 1. Custom GraphQL query
        string customQuery = @"{
            transactions(first: 10, tags: [{name: ""Type"", values: [""Process""]}]) {
                edges {
                    node {
                        id
                        recipient
                        tags { name value }
                    }
                }
            }
        }";
        
        var (success1, result1) = await graphqlHandler.SendGraphQLQueryAsync(customQuery);
        
        // 2. Get process transactions with helper method
        var tags = new List<Tag> { new Tag("Action", "Transfer") };
        var (success2, messages) = await graphqlHandler.GetProcessTransactionsAsync(
            processId: "your_process_id",
            additionalTags: tags,
            first: 20,
            getData: true,  // Fetch transaction data
            fromTimestamp: DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds()
        );
        
        // 3. Multiple endpoint fallback support
        graphqlHandler.graphqlEndpoints = new List<string>
        {
            "https://arweave.net/graphql",
            "https://arweave-search.goldsky.com/graphql",
            "https://g8way.io/graphql"
        };
    }
}
```

### EnqueueGraphQLHandler

For rate-limited GraphQL operations:

```csharp
public class EnqueuedGraphQLExample : MonoBehaviour
{
    public EnqueueGraphQLHandler enqueuedHandler;
    
    void Start()
    {
        // Configure queue settings
        enqueuedHandler.maxConcurrentRequests = 1;
        enqueuedHandler.enqueueRequestInterval = 1.0f;
        enqueuedHandler.maxQueueSize = 10;
        
        // Enqueue multiple queries - they'll be processed sequentially
        for (int i = 0; i < 5; i++)
        {
            string query = $"{{ transaction(id: \"tx_{i}\") {{ id tags {{ name value }} }} }}";
            enqueuedHandler.EnqueueGraphQLQuery(query, OnQueryResult);
        }
    }
    
    void OnQueryResult(bool success, string result)
    {
        if (success)
        {
            Debug.Log($"Query result: {result}");
        }
    }
}
```

---

## HyperBeam Integration

### What is HyperBEAM?

HyperBEAM is a high-performance message processing system for AO that offers:
- **Local processing** - Run compute units locally for instant responses
- **Caching** - Built-in response caching for repeated queries
- **Path-based routing** - RESTful-style API paths for intuitive access

Learn more: [HyperBEAM Documentation](https://hyperbeam.arweave.net/build/introduction/what-is-hyperbeam.html)

### Static Requests (Cached Data)

```csharp
public class HyperBeamStatic : MonoBehaviour
{
    public HyperBeamPathHandler hyperBeam;
    
    async void GetCachedData()
    {
        // Access cached data via static path
        var (success, result) = await hyperBeam.SendStaticRequestAsync(
            pid: "process_id",
            cachePath: "leaderboard/top100",
            now: false,  // Use cached data
            serialize: true
        );
        
        if (success)
        {
            var data = JSON.Parse(result);
            ProcessLeaderboard(data);
        }
    }
}
```

### Dynamic Requests (With Parameters)

```csharp
public class HyperBeamDynamic : MonoBehaviour
{
    public HyperBeamPathHandler hyperBeam;
    
    async void GetUserData(string userId)
    {
        // Fluent API for dynamic requests
        var (success, result) = await hyperBeam
            .ForProcess("process_id")
            .Method("GetUserInfo")
            .Parameter("UserId", userId)
            .Parameter("IncludeStats", "true")
            .Now(true)  // Use 'now' mode for speed
            .Serialize(true)
            .ExecuteAsync();
            
        if (success)
        {
            ProcessUserData(result);
        }
    }
}
```

---

## Unity Editor Testing

### Dual Mode Support

The Editor Tester supports both networking modes:

1. **Legacy AO Mode** - Test traditional AO messages with results
2. **HyperBEAM Mode** - Test fast HyperBEAM queries

### Setup Node.js Environment

1. **Navigate to EditorConnect**:
   ```bash
   cd Packages/com.permaverse.ao-sdk/EditorConnect~
   ```

2. **Run Setup Script**:
   - **macOS/Linux**: `./setup.sh`
   - **Windows**: `setup.bat`

3. **Configure Wallet**:
   - In AOConnectManager Inspector, set Editor Wallet Path
   - Browse to your Arweave wallet JSON keyfile

### Using AO Editor Tester

1. **Open Window**: Tools → Permaverse → AO Editor Tester

2. **Configure Settings**:
   - **Process ID**: Your AO process
   - **Message Mode**: 
     - **HyperBEAM** - Fast message processing
     - **Legacy** - Traditional AO messages with results
   - **Output Format**: Unity or Raw

3. **Send Messages**:
   ```csharp
   // Messages work in Editor with real wallet signing!
   async void TestInEditor()
   {
       var tags = new List<Tag> { new Tag("Action", "Test") };
       
       // Test Legacy AO
       var (success1, result1) = await messageHandler.SendRequestAsync(
           processId, tags, method: MessageHandler.NetworkMethod.Message
       );
       
       // Test HyperBEAM
       var (success2, result2) = await messageHandler.SendRequestAsync(
           processId, tags, method: MessageHandler.NetworkMethod.HyperBeamMessage
       );
       
       // Test Dryrun
       var (success3, result3) = await messageHandler.SendRequestAsync(
           processId, tags, method: MessageHandler.NetworkMethod.Dryrun
       );
   }
   ```

### Command Line Testing

```bash
# HyperBEAM mode
node aoconnect-editor.js \
  --process-id YOUR_PROCESS \
  --tag-Action=GetUserInfo \
  --mode hyperbeam \
  --log-level verbose

# Legacy AO mode (messages with results)
node aoconnect-editor.js \
  --process-id YOUR_PROCESS \
  --tag-Action=GetData \
  --mode legacy \
  --output unity
```

---

## Troubleshooting

### Network Mode Issues

**"Legacy AO message failed"**
- Check internet connection
- Ensure process ID is valid

**"HyperBEAM connection failed"**
- Verify HyperBEAM node is running: `curl http://localhost:8734`
- Check firewall settings
- Try remote HyperBEAM URL if local fails

**"Dryrun returns unexpected result"**
- Verify process ID and tags are correct
- Check if process state matches expectations
- Use Legacy AO mode to verify on-chain state

### GraphQL Issues

**"All GraphQL endpoints failed"**
- Check internet connectivity
- Verify query syntax is valid
- Try with a simple test query first
- Check if endpoints are responsive

**"GraphQL query timeout"**
- Reduce query complexity
- Use pagination for large datasets
- Increase timeout in handler settings

---

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/simonfriend/AO-Unity-SDK/issues)
- **AO Discord**: [Join the community](https://discord.gg/ao)
- **Documentation**: 
  - [AO Cookbook](https://cookbook_ao.ar.io/)
  - [HyperBEAM Docs](https://hyperbeam.arweave.net/build/introduction/what-is-hyperbeam.html)

---

## Next Steps

1. ✅ Complete the setup
2. 📝 Review example scenes in Samples
3. 🔨 Build your first AO-powered feature
4. 🚀 Deploy to WebGL
5. 🎮 Ship your decentralized game!