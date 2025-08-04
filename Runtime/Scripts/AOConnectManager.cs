using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
		}
	}

	// Track which wallet is currently active for signing
	public enum WalletType { None, Default, Arweave, EVM }

	public class AOConnectManager : MonoBehaviour
	{
		public string editorAddress;
		public static AOConnectManager main { get; private set; }

		// Store both wallet infos
		[SerializeField] private AddressInfo arweaveAddressInfo;
		[SerializeField] private AddressInfo evmAddressInfo;

		// Track the order of connected wallets, first is main
		private List<WalletType> connectedWallets = new List<WalletType>();

		// Old addressInfo is now obsolete, but keep for backward compatibility
		[Obsolete][SerializeField] private AddressInfo addressInfo;

		// Main wallet is always the first in the list
		public WalletType MainWalletType => connectedWallets.Count > 0 ? connectedWallets[0] : WalletType.None;

		// Helper to get AddressInfo by WalletType
		private AddressInfo GetAddressInfo(WalletType type)
		{
			switch (type)
			{
				case WalletType.Arweave: return arweaveAddressInfo;
				case WalletType.EVM: return evmAddressInfo;
				default: return null;
			}
		}

		// Main wallet info/properties
		public string CurrentAddress => GetAddressInfo(MainWalletType)?.address;
		public string CurrentBazarID => GetAddressInfo(MainWalletType)?.bazarProfileData?.Profile?.Id;
		public BazarProfileData CurrentBazarProfileData => GetAddressInfo(MainWalletType)?.bazarProfileData;
		public List<string> CurrentAddresses => GetAddressInfo(MainWalletType)?.allAddresses;
		public SessionKeyInfo CurrentSessionKeyInfo => GetAddressInfo(MainWalletType)?.sessionKeyInfo;
		public string CurrentSessionAddress => GetAddressInfo(MainWalletType)?.sessionKeyInfo?.address;

		// Helper: check if a wallet is connected
		public bool IsWalletConnected(WalletType type) => connectedWallets.Contains(type);

		// Helper: get secondary wallet info (if needed)
		public AddressInfo GetSecondaryWalletInfo(WalletType type) => GetAddressInfo(type);

		public Action OnCurrentAddressChange;

		// Add per-wallet-type connection events
		public event Action<WalletType> OnWalletConnected;

		[Space]

		public bool addClientVersionTag = false;
		public string clientVersion = "0.0.1";

		[Header("Settings")]
		public bool checkNotifications;

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
			}

			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
		}

		IEnumerator Start()
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
				yield return new WaitForSeconds(1);
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
				UpdateUIPanels(forceConnected:true);
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

			// // Clear all connected wallets
			// connectedWallets.Clear();
			// arweaveAddressInfo = null;
			// evmAddressInfo = null;

			// OnMainAddressChanged();
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
	}
}