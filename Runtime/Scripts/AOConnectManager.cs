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
	public class AddressInfo
	{
		public string address;
		public List<string> allAddresses;
		public BazarProfileData bazarProfileData;

		public AddressInfo(string wallet)
		{
			address = wallet;
		}
	}

	public class AOConnectManager : MonoBehaviour
	{
		public string editorAddress;
		public static AOConnectManager main { get; private set; }
		public string CurrentAddress => addressInfo != null ? addressInfo.address : null;
		public string CurrentBazarID => addressInfo != null && addressInfo.bazarProfileData != null ? addressInfo.bazarProfileData.Profile.Id : null;
		public BazarProfileData CurrentBazarProfileData => addressInfo != null && addressInfo.bazarProfileData != null ? addressInfo.bazarProfileData : null;
		public List<string> CurrentAddresses => addressInfo != null && addressInfo.allAddresses != null && addressInfo.allAddresses.Count > 0 ? addressInfo.allAddresses : null;

		public Action OnCurrentAddressChange;

		[SerializeField] private AddressInfo addressInfo;

		[Header("Settings")]
		public bool checkNotifications;

		[Header("UI")]
		public GameObject walletConnectedPanel;
		public GameObject walletNotConnectedPanel;
		public Button connectWalletButton;
		public Button connectMetamaskButton;
		public TMP_Text activeWalletText;

		public BazarMessageHandler bazarMessageHandler;

	

		[DllImport("__Internal")]
		private static extern void SendMessageCustomCallbackJS(string pid, string data, string tags, string id, string objectCallback, string methodCallback);

		[DllImport("__Internal")]
		private static extern void ConnectWalletJS();

		[DllImport("__Internal")]
		private static extern void ConnectMetamaskJS();

		[DllImport("__Internal")]
		private static extern void AlertMessageJS(string message);

		[DllImport("__Internal")]
		private static extern void CheckNotificationPermissionJS();

		[DllImport("__Internal")]
		private static extern void SendNotificationJS(string title, string text);

		[DllImport("__Internal")]
		private static extern void RefreshPage();

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
			connectWalletButton?.onClick.AddListener(() => ConnectWallet());
			connectMetamaskButton?.onClick.AddListener(ConnectMetamask);

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
				UpdateWallet(json.ToString());
			}

			if (bazarMessageHandler != null) bazarMessageHandler.OnBazarDataReceived += OnBazarInfoReceived;

			walletConnectedPanel?.SetActive(!string.IsNullOrEmpty(CurrentAddress));
			walletNotConnectedPanel?.SetActive(string.IsNullOrEmpty(CurrentAddress));
		}

		public void UpdateAddressInfo(AddressInfo info)
		{
			addressInfo = info;
			OnCurrentAddressChanged();
			OnCurrentAddressChange?.Invoke();
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

		public void ConnectWallet(bool forceMetamask = false) //Ask if possible to check through arconnect when someone changes wallet
		{
			if (useMetamask || forceMetamask)
			{
				ConnectMetamask();
			}
			else
			{
				ConnectWalletJS();
			}
		}

		private bool useMetamask = false;

		public void ConnectMetamask()
		{
			ConnectMetamaskJS();
		}

		public void SendMessageToProcess(string pid, string data, string tags, string id, string objectCallback = "AOConnectManager", string methodCallback = "MessageCallback")
		{
			if (CurrentAddress != null)
			{
				SendMessageCustomCallbackJS(pid, data, tags, id, objectCallback, methodCallback);
			}
		}

		private void OnCurrentAddressChanged()
		{
			if (activeWalletText != null)
			{
				if (string.IsNullOrEmpty(CurrentAddress))
				{
					activeWalletText.text = "Please connect wallet";
				}
				else
				{
					activeWalletText.text = CurrentAddress;
				}
			}

			walletConnectedPanel?.SetActive(!string.IsNullOrEmpty(CurrentAddress));
			walletNotConnectedPanel?.SetActive(string.IsNullOrEmpty(CurrentAddress));
		}

		private void OnBazarInfoReceived(BazarProfileData bazarProfileData)
		{
			if (bazarProfileData == null) return;

			if (bazarProfileData.Owner != CurrentAddress)
			{
				Debug.LogError("Bazar info for different address");
				return;
			}

			addressInfo.bazarProfileData = bazarProfileData;
		}

		// CALLBACKS FROM JS
		public void UpdateWallet(string walletData)
		{
			if (walletData == "Error")
			{
				Debug.LogError("Error with Wallet!!!");
				RefreshPage();
				return;
			}

			if (string.IsNullOrEmpty(walletData))
			{
				Debug.LogError("Wallet is null!!!");
				RefreshPage();
				return;
			}

			JSONNode address = JSON.Parse(walletData);

			if (!address.HasKey("address"))
			{
				Debug.LogError("Wallet is null!!!");
				RefreshPage();
				return;
			}

			string wallet = address["address"];

			AddressInfo info = new AddressInfo(wallet);

			JSONArray addressesArray = address["addresses"].AsArray;

			// Create a list to hold the addresses
			List<string> addressesList = new List<string>();

			// Loop through the array and add each address to the list
			foreach (JSONNode addressNode in addressesArray)
			{
				addressesList.Add(addressNode.Value);
			}

			info.allAddresses = addressesList;

			if (info.address != CurrentAddress)
			{
				UpdateAddressInfo(info);

				if (bazarMessageHandler != null)
				{
					bazarMessageHandler.GetBazarInfo(CurrentAddress);
				}

				if (AOUtils.IsEVMWallet(CurrentAddress))
				{
					useMetamask = true;
				}
			}
			else
			{
				Debug.Log("Same Wallet: " + wallet);
			}
		}

		public void MessageCallback(string result)
		{

		}
	}
}