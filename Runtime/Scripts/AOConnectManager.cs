using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;

namespace Permaverse.AO
{
	[Serializable]
	public class AOProcess
	{
		public string id;
		public string name;
		public string shortId;

		public Dictionary<string, string> tags = new Dictionary<string, string>();
	}

	[Serializable]
	public class SessionKeyInfo
	{
		public string address;
		public string mainWallet;
		public string expiryDate;
	}

	[Serializable]
	public class AddressInfo
	{
		public WalletType type;
		public string address;
		public List<string> allAddresses;
		public BazarProfileData bazarProfileData;
		public SessionKeyInfo sessionKeyInfo;

		public AddressInfo(string wallet)
		{
			address = wallet;
			sessionKeyInfo = null;
			bazarProfileData = null;
		}
	}

	// Track which wallet is currently active for signing
	public enum WalletType { None, Default, Arweave, EVM }

	public class AOConnectManager : MonoBehaviour
	{
		public string editorAddress;
		public static AOConnectManager main { get; private set; }

		/// <summary>
		/// Store wallet information for different wallet types
		/// </summary>
		[SerializeField] private AddressInfo arweaveAddressInfo;
		[SerializeField] private AddressInfo evmAddressInfo;

		/// <summary>
		/// Track the order of connected wallets (first is main wallet)
		/// </summary>
		private List<WalletType> connectedWallets = new List<WalletType>();

		/// <summary>
		/// Main wallet type (first connected wallet)
		/// </summary>
		public WalletType MainWalletType => connectedWallets.Count > 0 ? connectedWallets[0] : WalletType.None;

		/// <summary>
		/// Get AddressInfo by wallet type
		/// </summary>
		private AddressInfo GetAddressInfo(WalletType type)
		{
			switch (type)
			{
				case WalletType.Arweave: return arweaveAddressInfo;
				case WalletType.EVM: return evmAddressInfo;
				case WalletType.Default: return GetAddressInfo(MainWalletType);
				default: return null;
			}
		}

		/// <summary>
		/// Main wallet properties for easy access
		/// </summary>
		public string CurrentAddress => GetAddressInfo(MainWalletType)?.address;
		public string CurrentBazarID => GetAddressInfo(MainWalletType)?.bazarProfileData?.Profile?.Id;
		public BazarProfileData CurrentBazarProfileData => GetAddressInfo(MainWalletType)?.bazarProfileData;
		public List<string> CurrentAddresses => GetAddressInfo(MainWalletType)?.allAddresses;
		public SessionKeyInfo CurrentSessionKeyInfo => GetAddressInfo(MainWalletType)?.sessionKeyInfo;
		public string CurrentSessionAddress => CurrentSessionKeyInfo?.address;

		/// <summary>
		/// Check if a specific wallet type is connected
		/// </summary>
		public bool IsWalletConnected(WalletType type) => connectedWallets.Contains(type);

		/// <summary>
		/// Get secondary wallet information (for multi-wallet scenarios)
		/// </summary>
		public AddressInfo GetSecondaryWalletInfo(WalletType type) => GetAddressInfo(type);

		/// <summary>
		/// Event fired when the current address changes
		/// </summary>
		public Action OnCurrentAddressChange;

		/// <summary>
		/// Event fired when a wallet of specific type is connected
		/// </summary>
		public event Action<WalletType> OnWalletConnected;

		[Space]

		public bool addClientVersionTag = false;
		public string clientVersion = "0.0.1";

		[Header("Settings")]
		public bool checkNotifications;
		public bool showLogs = false;

		[Header("HyperBEAM")]
		[Tooltip("URL for HyperBEAM service endpoint")]
		public string hyperBeamUrl = "http://localhost:8734";

		[Header("Editor Testing")]
		[Tooltip("Path to Arweave wallet keyfile for editor testing")]
		[HideInInspector] public string editorWalletPath = "";

		[Header("UI")]
		public GameObject walletConnectedPanel;
		public GameObject walletNotConnectedPanel;
		public Button connectWalletButton;
		public Button connectMetamaskButton;
		public Button logoutButton;
		public TMP_Text activeWalletText;

		public BazarMessageHandler bazarMessageHandler;

		private Dictionary<WalletType, List<Action>> pendingWalletActions = new Dictionary<WalletType, List<Action>>();

		[DllImport("__Internal")]
		private static extern void SendMessageCustomCallbackJS(string pid, string data, string tags, string id, string objectCallback, string methodCallback, string useMainWallet, string chain);

		[DllImport("__Internal")]
		private static extern void SendMessageHyperBeamJS(string pid, string data, string tags, string id, string objectCallback, string methodCallback, string useMainWallet, string chain, string hyperBeamUrl);

		[DllImport("__Internal")]
		private static extern void ConnectWalletJS();

		[DllImport("__Internal")]
		private static extern void ConnectMetamaskJS();

		[DllImport("__Internal")]
		private static extern void OpenWanderConnectJS();

		[DllImport("__Internal")]
		private static extern void CloseWanderConnectJS();

		[DllImport("__Internal")]
		private static extern void SignOutWanderJS();

		[DllImport("__Internal")]
		private static extern void AlertMessageJS(string message);

		[DllImport("__Internal")]
		private static extern void CheckNotificationPermissionJS();

		[DllImport("__Internal")]
		private static extern void SendNotificationJS(string title, string text);

		[DllImport("__Internal")]
		private static extern void RefreshPage();

		[DllImport("__Internal")]
		private static extern void CopyToClipboardJS(string text);

		void Awake()
		{
			if (main == null)
			{
				main = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

			if (!Application.isEditor)
			{
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					showLogs = UrlUtilities.GetUrlParameterValue("showLogs") == "true";
				}
			}
			else
			{
				showLogs = true;
			}
		}

		async void Start()
		{
			connectWalletButton?.onClick.AddListener(() => ConnectWallet(WalletType.Arweave));
			connectMetamaskButton?.onClick.AddListener(() => ConnectWallet(WalletType.EVM));
			logoutButton?.onClick.AddListener(() => SignOutWander());

			if (!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
			{
				if (checkNotifications)
				{
					CheckNotificationPermissionJS();
				}
			}

			// For test in editor
			if (Application.isEditor && !string.IsNullOrEmpty(editorAddress))
			{
				await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: this.GetCancellationTokenOnDestroy());
				JSONObject json = new JSONObject();
				json["address"] = editorAddress;
				json["chain"] = "arweave";
				UpdateWallet(json.ToString());
			}

			if (bazarMessageHandler != null) bazarMessageHandler.OnBazarDataReceived += OnBazarInfoReceived;

			UpdateUIPanels();
		}

		// New connect method
		public void ConnectWallet(WalletType walletType)
		{
			if (Application.isEditor)
			{
				Debug.Log("Connecting wallet in editor: " + walletType);
				return;
			}
			// Only connect if not already connected
			if (!connectedWallets.Contains(walletType))
			{
				if (walletType == WalletType.EVM)
				{
					ConnectMetamaskJS();
				}
				else if (walletType == WalletType.Arweave)
				{
					ConnectWalletJS();
				}
			}
			else
			{
				Debug.Log("Wallet already connected: " + walletType);
			}
		}

		// Update info for a specific wallet type
		public void UpdateAddressInfo(AddressInfo info, string chain)
		{
			WalletType type = chain == "evm" ? WalletType.EVM : WalletType.Arweave;

			switch (type)
			{
				case WalletType.EVM:
					evmAddressInfo = info;
					break;
				case WalletType.Arweave:
					arweaveAddressInfo = info;
					break;
				default:
					Debug.LogError("Unknown wallet type: " + type);
					return;
			}

			info.type = type;

			// Add to connectedWallets if not present
			if (!connectedWallets.Contains(type))
			{
				connectedWallets.Add(type);
				OnNewWalletConnected(type);
				// Fire event for this wallet type
				OnWalletConnected?.Invoke(type);
			}

			// Only trigger UI/events if this is the first wallet (main)
			if (type == MainWalletType)
			{
				OnMainAddressChanged();
				OnCurrentAddressChange?.Invoke();
			}
		}

		// Update UI panels based on both wallets
		private void UpdateUIPanels(bool forceConnected = false)
		{
			bool anyConnected = forceConnected || connectedWallets.Count > 0;
			walletConnectedPanel?.SetActive(anyConnected);
			walletNotConnectedPanel?.SetActive(!anyConnected);
		}

		private void OnMainAddressChanged()
		{
			if (activeWalletText != null)
			{
				if (string.IsNullOrEmpty(CurrentAddress))
				{
					activeWalletText.text = "Please connect wallet";
				}
				else
				{
					activeWalletText.text = CurrentAddress + $" ({MainWalletType})";
				}
			}

			UpdateUIPanels();
		}

		private void OnBazarInfoReceived(BazarProfileData bazarProfileData)
		{
			if (bazarProfileData == null) return;

			if (bazarProfileData.Owner != CurrentAddress)
			{
				Debug.LogError("Bazar info for different address");
				return;
			}

			var info = GetAddressInfo(MainWalletType);
			if (info != null)
			{
				info.bazarProfileData = bazarProfileData;
			}
		}

		// CALLBACKS FROM JS
		public void UpdateWallet(string walletData)
		{
			if (walletData == "Error")
			{
				Debug.LogError("Error with Wallet!!!");
				if (string.IsNullOrEmpty(CurrentAddress))
				{
					RefreshPage();
				}
				return;
			}
			if (walletData == "Loading")
			{
				// Debug.Log("Wallet is loading...");
				UpdateUIPanels(forceConnected: true);
				return;
			}
			if (string.IsNullOrEmpty(walletData))
			{
				Debug.LogError("Wallet is null!!!");
				if (string.IsNullOrEmpty(CurrentAddress))
				{
					RefreshPage();
				}
				return;
			}
			Debug.Log("Wallet Data: " + walletData);

			JSONNode address = JSON.Parse(walletData);

			if (!address.HasKey("address"))
			{
				Debug.LogError("Wallet is null!!!");
				if (string.IsNullOrEmpty(CurrentAddress))
				{
					RefreshPage();
				}
				return;
			}

			string wallet = address["address"];
			string chain = address["chain"];

			if (string.IsNullOrEmpty(chain))
			{
				if (address.HasKey("sessionKey")) chain = "evm";
				else chain = "arweave";

				Debug.LogError("Chain is null!!!");
			}

			if (AOUtils.IsEVMWallet(wallet))
			{
				wallet = wallet.ToLower();
			}

			AddressInfo info = new AddressInfo(wallet);

			JSONArray addressesArray = address["addresses"].AsArray;
			List<string> addressesList = new List<string>();
			foreach (JSONNode addressNode in addressesArray)
			{
				string addressString = addressNode.Value;
				if (AOUtils.IsEVMWallet(addressString))
				{
					addressString = addressString.ToLower();
				}
				addressesList.Add(addressString);
			}
			info.allAddresses = addressesList;

			if (address.HasKey("sessionKey"))
			{
				SessionKeyInfo sessionKeyInfo = new SessionKeyInfo();
				sessionKeyInfo.address = address["sessionKey"]["address"].Value.ToLower();
				sessionKeyInfo.mainWallet = address["sessionKey"]["mainWallet"].Value.ToLower();
				sessionKeyInfo.expiryDate = address["sessionKey"]["expiry"];
				info.sessionKeyInfo = sessionKeyInfo;
			}
			else
			{
				info.sessionKeyInfo = null;
				Debug.Log("No session key info");
			}

			UpdateAddressInfo(info, chain);

			if (bazarMessageHandler != null && MainWalletType != WalletType.None && info.address == CurrentAddress)
			{
				bazarMessageHandler.GetBazarInfo(info.address);
			}
		}

		private void OnNewWalletConnected(WalletType type)
		{
			if (pendingWalletActions.TryGetValue(type, out var actions))
			{
				foreach (var action in actions)
					action?.Invoke();
				actions.Clear();
			}
		}

		// Update SendMessageToProcess to allow specifying wallet type
		public void SendMessageToProcess(string pid, string data, string tags, string id, string objectCallback = "AOConnectManager", string methodCallback = "MessageCallback", bool useMainWallet = false, WalletType typeToUse = WalletType.Default)
		{
			// In editor, use Node.js script if wallet path is configured
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(editorWalletPath) && System.IO.File.Exists(editorWalletPath))
			{
				// Use async execution to avoid blocking the main thread
				SendMessageViaNodeScriptAsync(pid, data, tags, id, null, objectCallback, methodCallback, isLegacyMode: true, this.GetCancellationTokenOnDestroy()).Forget();
				return;
			}
			else
			{
				Debug.LogError("[AOConnectManager] Cannot send legacy message in Editor: No wallet keyfile configured or file not found.\n" +
					"Please set the 'Editor Wallet Path' field in the AOConnectManager component to point to a valid Arweave wallet keyfile.");
				return;
			}
#else
			if (typeToUse != WalletType.Default && !IsWalletConnected(typeToUse))
			{
				// Queue the action
				if (!pendingWalletActions.ContainsKey(typeToUse))
					pendingWalletActions[typeToUse] = new List<Action>();

				pendingWalletActions[typeToUse].Add(() =>
					SendMessageToProcess(pid, data, tags, id, objectCallback, methodCallback, useMainWallet, typeToUse)
				);

				// Trigger connection
				ConnectWallet(typeToUse);
				return;
			}

			string useMainWalletString = useMainWallet ? "true" : "false";
			string chain = typeToUse.ToString().ToLower();
			SendMessageCustomCallbackJS(pid, data, tags, id, objectCallback, methodCallback, useMainWalletString, chain);
#endif
		}

		public void SendMessageToProcessHyperBeam(string pid, string data, string tags, string id, string hyperBeamUrl, string objectCallback = "AOConnectManager", string methodCallback = "MessageCallback", bool useMainWallet = false, WalletType typeToUse = WalletType.Default)
		{
			// In editor, use Node.js script if wallet path is configured
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(editorWalletPath) && System.IO.File.Exists(editorWalletPath))
			{
				// Use async execution to avoid blocking the main thread
				SendMessageViaNodeScriptAsync(pid, data, tags, id, hyperBeamUrl, objectCallback, methodCallback, isLegacyMode: false, this.GetCancellationTokenOnDestroy()).Forget();
				return;
			}
			else
			{
				Debug.LogError("[AOConnectManager] Cannot send HyperBEAM message in Editor: No wallet keyfile configured or file not found.\n" +
					"Please set the 'Editor Wallet Path' field in the AOConnectManager component to point to a valid Arweave wallet keyfile.");
				return;
			}
#else
			if (typeToUse != WalletType.Default && !IsWalletConnected(typeToUse))
			{
				// Queue the action
				if (!pendingWalletActions.ContainsKey(typeToUse))
					pendingWalletActions[typeToUse] = new List<Action>();

				pendingWalletActions[typeToUse].Add(() =>
					SendMessageToProcessHyperBeam(pid, data, tags, id, hyperBeamUrl, objectCallback, methodCallback, useMainWallet, typeToUse)
				);

				// Trigger connection
				ConnectWallet(typeToUse);
				return;
			}
			string useMainWalletString = useMainWallet ? "true" : "false";
			string chain = typeToUse.ToString().ToLower();
			SendMessageHyperBeamJS(pid, data, tags, id, objectCallback, methodCallback, useMainWalletString, chain, hyperBeamUrl);
#endif
		}

		public void OpenWanderConnect()
		{
			if (!Application.isEditor)
			{
				OpenWanderConnectJS();
			}
			else
			{
				Debug.Log("Opening Wander Connect in editor");
			}
		}

		public void CloseWanderConnect()
		{
			if (!Application.isEditor)
			{
				CloseWanderConnectJS();
			}
			else
			{
				Debug.Log("Closing Wander Connect in editor");
			}
		}

		public void SignOutWander()
		{
			if (!Application.isEditor)
			{
				SignOutWanderJS();
			}
			else
			{
				Debug.Log("Signing out Wander in editor");
			}

			RefreshPage();
		}

		public void SendNotification(string title, string text)
		{
			if (!Application.isEditor)
			{
				SendNotificationJS(title, text);
			}
		}

		public void OpenAlert(string message)
		{
			if (!Application.isEditor)
			{
				AlertMessageJS(message);
			}
			else
			{
				Debug.LogError("Alert: " + message);
			}
		}

		public void RefreshWebPage()
		{
			RefreshPage();
		}

		public void CopyToClipboard(string text)
		{
			if (!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
			{
				CopyToClipboardJS(text);
			}
			else
			{
				// For editor/testing, you can use Unity's GUIUtility as fallback
				GUIUtility.systemCopyBuffer = text;
				Debug.Log("Copied to clipboard (Editor): " + text);
			}
		}

		public void MessageCallback(string result)
		{

		}

#if UNITY_EDITOR
		// Helper method to ensure we always call the callback with proper error formatting
		private void InvokeCallback(string objectCallback, string methodCallback, string id, string errorMessage = null, string output = null)
		{
			string json;

			if (!string.IsNullOrEmpty(errorMessage))
			{
				// Properly escape error message for JSON
				string escapedError = errorMessage
					.Replace("\\", "\\\\")  // Backslash first!
					.Replace("\"", "\\\"")  // Quotes
					.Replace("\n", "\\n")   // Newlines
					.Replace("\r", "\\r")   // Carriage returns
					.Replace("\t", "\\t");  // Tabs
				
				// Create error response in the same format as JavaScript functions
				json = $"{{\"Messages\":[],\"Spawns\":[],\"Output\":\"\",\"Error\":\"{escapedError}\",\"uniqueID\":\"{id}\"}}";
			}
			else if (!string.IsNullOrEmpty(output))
			{
				json = output;
			}
			else
			{
				// Fallback error case
				json = $"{{\"Messages\":[],\"Spawns\":[],\"Output\":\"\",\"Error\":\"Unknown error in SendMessageViaNodeScript\",\"uniqueID\":\"{id}\"}}";
			}

			// Find the target object and call the callback method
			GameObject targetObject = GameObject.Find(objectCallback);
			if (targetObject != null)
			{
				var component = targetObject.GetComponent<MonoBehaviour>();
				if (component != null)
				{
					var method = component.GetType().GetMethod(methodCallback);
					if (method != null)
					{
						method.Invoke(component, new object[] { json });
					}
					else
					{
						Debug.LogWarning($"[AOConnectManager] Callback method '{methodCallback}' not found on {objectCallback}");
					}
				}
				else
				{
					Debug.LogWarning($"[AOConnectManager] Component not found on {objectCallback}");
				}
			}
			else
			{
				Debug.LogWarning($"[AOConnectManager] Callback object '{objectCallback}' not found");
			}
		}

		/// <summary>
		/// Async version of SendMessageViaNodeScript using UniTask
		/// </summary>
		private async UniTask SendMessageViaNodeScriptAsync(string pid, string data, string tags, string id, string hyperBeamUrl, string objectCallback, string methodCallback, bool isLegacyMode = false, CancellationToken cancellationToken = default)
		{
			try
			{
				string modeLabel = isLegacyMode ? "Legacy AO" : "HyperBEAM";
				Debug.Log($"[AOConnectManager] Sending {modeLabel} message via Node.js script in editor");

				// Build command arguments
				var arguments = new List<string>();

				// Add script path
				string scriptPath = GetNodeScriptPath();
				if (string.IsNullOrEmpty(scriptPath))
				{
					string errorMsg = "aoconnect-editor.js script not found";
					Debug.LogError($"[AOConnectManager] {errorMsg}");
					InvokeCallback(objectCallback, methodCallback, id, errorMsg);
					return;
				}

				arguments.Add(scriptPath);

				// Add configuration
				arguments.Add("--process-id");
				arguments.Add(pid);
				arguments.Add("--wallet");
				arguments.Add(editorWalletPath);
				arguments.Add("--output");
				arguments.Add("unity");
				arguments.Add("--mode");
				arguments.Add(isLegacyMode ? "legacy" : "hyperbeam");
				arguments.Add("--log-level");
				arguments.Add("none"); // Use silent mode for production usage

				// Add HyperBEAM URL only for HyperBEAM mode
				if (!isLegacyMode && !string.IsNullOrEmpty(hyperBeamUrl))
				{
					arguments.Add("--hyperbeam-url");
					arguments.Add(hyperBeamUrl);
				}

				// Add unique ID for request/response correlation
				if (!string.IsNullOrEmpty(id))
				{
					arguments.Add("--unique-id");
					arguments.Add(id);
				}

				// Add data if provided using base64 encoding to avoid command line escaping issues
				if (!string.IsNullOrEmpty(data))
				{
					// Encode data as base64 to avoid command line escaping issues with JSON
					byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
					string dataBase64 = Convert.ToBase64String(dataBytes);

					arguments.Add("--data-base64");
					arguments.Add(dataBase64);
					Debug.Log($"[AOConnectManager] Encoding data as base64 ({dataBytes.Length} bytes)");
					Debug.Log($"[AOConnectManager] Data: {data}");
				}

				// Parse and add tags using base64 encoding for the entire tags object
				if (!string.IsNullOrEmpty(tags))
				{
					try
					{
						var tagsJson = SimpleJSON.JSON.Parse(tags);
						if (tagsJson.IsArray)
						{
							// Convert tags array directly to JSON object format for base64 encoding
							var tagsObjectJson = new SimpleJSON.JSONObject();
							for (int i = 0; i < tagsJson.AsArray.Count; i++)
							{
								var tagNode = tagsJson.AsArray[i];
								if (tagNode.HasKey("name") && tagNode.HasKey("value"))
								{
									string tagName = tagNode["name"];
									string tagValue = tagNode["value"];
									tagsObjectJson[tagName] = tagValue;
								}
							}

							// Encode tags object as base64
							string tagsObjectJsonString = tagsObjectJson.ToString();
							byte[] tagsBytes = System.Text.Encoding.UTF8.GetBytes(tagsObjectJsonString);
							string tagsBase64 = Convert.ToBase64String(tagsBytes);

							arguments.Add("--tags-base64");
							arguments.Add(tagsBase64);
							Debug.Log($"[AOConnectManager] Encoding tags as base64: {tagsObjectJsonString}");
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"[AOConnectManager] Failed to parse tags: {e.Message}");
					}
				}

				// Execute Node.js script asynchronously using NodeJsUtils
				string output = await NodeJsUtils.ExecuteNodeScriptAsync(arguments.Select(arg => $"\"{arg}\"").ToArray());

				Debug.Log($"[AOConnectManager] Node.js script completed successfully. Output: {output}");

				// Parse response - Node.js script should return the same format as JavaScript sendMessageHyperBeam
				// Expected format: { "Messages": [], "Spawns": [], "Output": "", "Error": null, "uniqueID": "..." }
				var response = JSON.Parse(output);

				// Call the callback with the successful response
				InvokeCallback(objectCallback, methodCallback, id, null, output);

				// Log any errors from the response
				if (response.HasKey("Error") && !response["Error"].IsNull && !string.IsNullOrEmpty(response["Error"]))
				{
					Debug.LogWarning($"[AOConnectManager] HyperBEAM response contains error: {response["Error"]}");
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested - no need to log
			}
			catch (Exception e)
			{
				string errorMsg = $"Failed to execute Node.js script: {e.Message}";
				Debug.LogError($"[AOConnectManager] {errorMsg}");
				InvokeCallback(objectCallback, methodCallback, id, errorMsg);
			}
		}	

		private string GetNodeScriptPath()
		{
			// Look for the script in the AO SDK EditorConnect directory
			string[] searchPaths = {
				System.IO.Path.Combine(Application.dataPath, "..", "Packages", "com.permaverse.ao-sdk", "EditorConnect~", "aoconnect-editor.js"),
				System.IO.Path.Combine(Application.dataPath, "Packages", "com.permaverse.ao-sdk", "EditorConnect~", "aoconnect-editor.js"),
				System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Packages", "com.permaverse.ao-sdk", "EditorConnect~", "aoconnect-editor.js")
			};

			foreach (string path in searchPaths)
			{
				if (System.IO.File.Exists(path))
				{
					return path;
				}
			}

			return null;
		}
#endif
	}
}