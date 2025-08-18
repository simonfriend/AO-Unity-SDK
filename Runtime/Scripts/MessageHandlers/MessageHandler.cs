using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace Permaverse.AO
{
	public class MessageHandler : MonoBehaviour
	{
		[Header("Debug")]
		public bool showLogs = true;

		[Header("ResendSettings")]
		public bool resendIfMissingMessages = true;
		public bool resendIfMissingTargetMessage = true;
		public int targetMessageIndex = 0;
		public bool resendIfTargetMessageNoData = true;
		public bool resendIfResultFalse = true;
		public List<int> resendDelays = new List<int> { 3, 30, 60 };
		public bool increaseResendDelay = true;
		public float refreshAfterTimeInError = 10 * 60;
		public bool refreshEnabled = true;

		public bool doWeb2IfInEditor = false;

		private float elapsedTimeSinceFirstErrorMessage = 0;


		private int resendIndex = 0;
		protected string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";

		[Header("HyperBEAM Settings")]
		[Tooltip("Override HyperBEAM URL. If empty, uses AOConnectManager.main.hyperBeamUrl")]
		public string hyperBeamUrlOverride = "";  // Empty = use AOConnectManager default
		public string luaModuleId = "";
		public bool fallbackToLegacy = true;

		// Property to get effective HyperBEAM URL
		protected string HyperBeamUrl =>
			!string.IsNullOrEmpty(hyperBeamUrlOverride) ? hyperBeamUrlOverride : AOConnectManager.main.hyperBeamUrl;

		protected int timeout = 120;

		protected Dictionary<string, (bool, string, string)> results = new Dictionary<string, (bool, string, string)>();
		protected int requestsCount = 0;

		public enum NetworkMethod
		{
			Dryrun,
			Message,
			HyperBeamMessage
		}

		private void Start()
		{
			if (!Application.isEditor)
			{
				showLogs = UrlUtilities.GetUrlParameterValue("showLogs") == "true";
			}
			else
			{
				showLogs = true;
			}
		}

		private void Update()
		{
			if (resendIndex > 0 && refreshEnabled)
			{
				elapsedTimeSinceFirstErrorMessage += Time.deltaTime;
				if (elapsedTimeSinceFirstErrorMessage > refreshAfterTimeInError)
				{
					elapsedTimeSinceFirstErrorMessage = 0;
					AOConnectManager.main.RefreshWebPage();
				}
			}
			else
			{
				elapsedTimeSinceFirstErrorMessage = 0;
			}
		}

		public virtual void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			StartCoroutine(SendRequestCoroutine(pid, tags, callback, data, method, useMainWallet, walletType));
		}

		public virtual void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, float delay, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			StartCoroutine(SendRequestCoroutineDelayed(pid, tags, callback, delay, data, method, useMainWallet, walletType));
		}

		public virtual void SendHyperBeamStaticRequest(string pid, string cachePath, Action<bool, string> callback, bool now = true, bool serialize = true, bool addCachePath = true)
		{
			string path = BuildHyperBeamStaticPath(pid, cachePath, now, serialize, addCachePath);
			StartCoroutine(SendHyperBeamPathCoroutine(path, callback));
		}

		public virtual void SendHyperBeamDynamicRequest(string pid, string methodName, List<Tag> parameters, Action<bool, string> callback, bool now = true, bool serialize = true, string moduleId = null)
		{
			string path = BuildHyperBeamDynamicPath(pid, methodName, parameters, now, serialize, moduleId);
			StartCoroutine(SendHyperBeamPathCoroutine(path, callback));
		}

		protected virtual IEnumerator SendRequestCoroutineDelayed(string pid, List<Tag> tags, Action<bool, NodeCU> callback, float delay, string data = "", NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			yield return new WaitForSeconds(delay);
			SendRequest(pid, tags, callback, data, method, useMainWallet, walletType);
		}

		protected virtual IEnumerator SendRequestCoroutine(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = "", NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			if (method == NetworkMethod.Dryrun)
			{
				yield return StartCoroutine(SendHttpPostRequest(pid, tags, callback, data, useMainWallet, walletType));
			}
			else if (method == NetworkMethod.HyperBeamMessage)
			{
				yield return StartCoroutine(SendHyperBeamMessage(pid, data, tags, callback, useMainWallet, walletType));
			}
			else if (!Application.isEditor)
			{
				yield return StartCoroutine(SendMessageToProcess(pid, data, tags, callback, useMainWallet, walletType));
			}
			else if (doWeb2IfInEditor)
			{
				yield return StartCoroutine(SendHttpPostRequest(pid, tags, callback, data, useMainWallet, walletType));
			}
			else
			{
				Debug.LogError($"[{gameObject.name}] Can't send messages in editor");
			}
		}

		protected virtual IEnumerator SendHyperBeamPathCoroutine(string url, Action<bool, string> callback)
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending HyperBEAM path request to: {url}");
			}

			UnityWebRequest request = UnityWebRequest.Get(url);
			request.timeout = timeout;

			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				if (showLogs) Debug.LogError($"[{gameObject.name}] HyperBEAM path Error: {request.error}");

				// Use retry logic before fallback
				if (resendIfResultFalse)
				{
					if (showLogs) Debug.Log($"[{gameObject.name}] Retrying HyperBEAM path request in {resendDelays[resendIndex]} seconds");

					StartCoroutine(RetryHyperBeamPathRequestDelayed(url, callback, resendDelays[resendIndex]));

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else if (fallbackToLegacy)
				{
					if (showLogs) Debug.Log($"[{gameObject.name}] Falling back to legacy dry-run");
					// TODO: Implement fallback logic
					callback?.Invoke(false, $"{{\"Error\":\"{request.error}\"}}");
				}
				else
				{
					callback?.Invoke(false, $"{{\"Error\":\"{request.error}\"}}");
				}
			}
			else
			{
				// Determine if response was serialized by checking URL
				bool wasSerialized = url.Contains("/serialize~json@1.0");
				string responseData = ParseHyperBeamResponse(request.downloadHandler.text, wasSerialized);

				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] HyperBEAM Result: {responseData}");
				}

				callback?.Invoke(true, responseData);
				resendIndex = 0; // Reset retry index on success
			}
		}

		protected virtual IEnumerator RetryHyperBeamPathRequestDelayed(string url, Action<bool, string> callback, float delay)
		{
			yield return new WaitForSeconds(delay);
			StartCoroutine(SendHyperBeamPathCoroutine(url, callback));
		}

		protected IEnumerator SendHttpPostRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = "", bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			string url = baseUrl + pid;
			string ownerId;
			if (useMainWallet || string.IsNullOrEmpty(AOConnectManager.main.CurrentSessionAddress))
			{
				ownerId = string.IsNullOrEmpty(AOConnectManager.main.CurrentAddress) ? "1234" : AOConnectManager.main.CurrentAddress;
			}
			else
			{
				ownerId = AOConnectManager.main.CurrentSessionAddress;
			}

			string jsonBody = CreateJsonBody(pid, ownerId, tags, data);

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending request | {jsonBody}");
			}

			UnityWebRequest request = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.timeout = timeout;
			request.SetRequestHeader("Content-Type", "application/json");

			yield return request.SendWebRequest();

			NodeCU jsonResponse;

			if (request.result != UnityWebRequest.Result.Success)
			{
				if (showLogs) Debug.LogError($"[{gameObject.name}] HTTP Post Error: {request.error} | {jsonBody}");

				jsonResponse = new NodeCU($"{{\"Error\":\"{request.error}\"}}");

				if (resendIfResultFalse)
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], method: NetworkMethod.Dryrun, useMainWallet: useMainWallet, walletType: walletType);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(false, jsonResponse);
				}
			}
			else
			{
				jsonResponse = new NodeCU(request.downloadHandler.text);

				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] HTTP Result : {request.downloadHandler.text}");
				}

				if (ShouldResend(jsonResponse))
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], method: NetworkMethod.Dryrun);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(true, jsonResponse);
					resendIndex = 0;
				}
			}
		}

		protected IEnumerator SendMessageToProcess(string pid, string data, List<Tag> tags, Action<bool, NodeCU> callback, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			string ownerId;

			if (useMainWallet || string.IsNullOrEmpty(AOConnectManager.main.CurrentSessionAddress))
			{
				ownerId = AOConnectManager.main.CurrentAddress;
			}
			else
			{
				ownerId = AOConnectManager.main.CurrentSessionAddress;
			}

			if (data == null)
			{
				data = "";
			}

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending message from {ownerId} to {pid} with data: {data}");
			}

			if (string.IsNullOrEmpty(ownerId))
			{
				Debug.LogError("Current address is null!!");
				var jsonResponse = new NodeCU("{\"Error\":\"Current address is null!\"}");
				callback?.Invoke(false, jsonResponse);
				yield break;
			}

			requestsCount++;
			string uniqueID = requestsCount.ToString();

			results[uniqueID] = (false, string.Empty, ownerId);

			JSONArray tagsJsonArray = new JSONArray();
			foreach (var tag in tags)
			{
				tagsJsonArray.Add(tag.ToJson());
			}

			if (AOConnectManager.main.addClientVersionTag && !string.IsNullOrEmpty(AOConnectManager.main.clientVersion))
			{
				tagsJsonArray.Add(new Tag("ClientVersion", AOConnectManager.main.clientVersion).ToJson());
			}

			string tagsStr = tagsJsonArray.ToString();

			AOConnectManager.main.SendMessageToProcess(pid, data, tagsStr, uniqueID, gameObject.name, "MessageCallback", useMainWallet, walletType);

			yield return new WaitUntil(() => results[uniqueID].Item1);

			var (result, response, savedAddress) = results[uniqueID];

			NodeCU networkResponse = new NodeCU(response);

			string currentOwnerId;

			if (useMainWallet || string.IsNullOrEmpty(AOConnectManager.main.CurrentSessionAddress))
			{
				currentOwnerId = AOConnectManager.main.CurrentAddress;
			}
			else
			{
				currentOwnerId = AOConnectManager.main.CurrentSessionAddress;
			}

			if (savedAddress != currentOwnerId)
			{
				if (showLogs)
				{
					Debug.LogError("Address mismatch between request and response.");
				}

				networkResponse = new NodeCU("{\"Error\":\"Address mismatch between request and response.\"}");

				if (resendIfResultFalse)
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], data, method: NetworkMethod.Message, useMainWallet: useMainWallet);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(false, networkResponse);
					resendIndex = 0;
				}
			}
			else
			{
				if (!networkResponse.IsSuccessful() && resendIfResultFalse)
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], data, method: NetworkMethod.Message, useMainWallet: useMainWallet);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(networkResponse.IsSuccessful(), networkResponse);
					resendIndex = 0;
				}
			}

			results.Remove(uniqueID);
		}

		public void MessageCallback(string jsonResult)
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Result: {jsonResult}");
			}

			var result = JSON.Parse(jsonResult);
			string uniqueID = result["uniqueID"];

			if (results.ContainsKey(uniqueID))
			{
				results[uniqueID] = (true, jsonResult, results[uniqueID].Item3);
			}
		}

		protected bool ShouldResend(NodeCU response)
		{
			if (resendIfMissingMessages && response.Messages.Count == 0)
			{
				Debug.LogError($"[{gameObject.name}] No Data, retrying");
				return true;
			}
			else if (resendIfMissingTargetMessage && response.Messages.Count <= targetMessageIndex)
			{
				Debug.LogError($"[{gameObject.name}] No Message[{targetMessageIndex}], retrying");
				return true;
			}
			else if (resendIfTargetMessageNoData && response.Messages[targetMessageIndex].Data == null)
			{
				Debug.LogError($"[{gameObject.name}] No Message[{targetMessageIndex}], retrying");
				return true;
			}
			else
			{
				return false;
			}
		}

		protected string CreateJsonBody(string pid, string ownerId, List<Tag> tags, string data = "")
		{
			JSONObject json = new JSONObject();
			json["Id"] = "1234";
			json["Target"] = pid;
			json["Owner"] = ownerId;

			if (!string.IsNullOrEmpty(data))
			{
				json["Data"] = data;
			}

			JSONArray tagsArray = new JSONArray();
			foreach (Tag tag in tags)
			{
				tagsArray.Add(tag.ToJson());
			}
			json["Tags"] = tagsArray;

			return json.ToString();
		}

		protected string BuildHyperBeamStaticPath(string pid, string cachePath, bool now, bool serialize, bool addCachePath)
		{
			string baseUrl = $"{HyperBeamUrl}/{pid}~process@1.0/{(now ? "now" : "compute")}";
			string cachePrefix = addCachePath ? "/cache" : "";
			string serializeSuffix = serialize ? "/serialize~json@1.0" : "";
			return $"{baseUrl}{cachePrefix}/{cachePath}{serializeSuffix}";
		}

		protected string BuildHyperBeamDynamicPath(string pid, string methodName, List<Tag> parameters, bool now, bool serialize, string moduleId = null)
		{
			string baseUrl = $"{HyperBeamUrl}/{pid}~process@1.0/{(now ? "now" : "compute")}";
			string effectiveModuleId = moduleId ?? luaModuleId; // Use override or default
			string paramString = EncodeParameters(parameters);
			string serializeSuffix = serialize ? "/serialize~json@1.0" : "";
			return $"{baseUrl}/~lua@5.3a&module={effectiveModuleId}{paramString}/{methodName}{serializeSuffix}";
		}

		protected string EncodeParameters(List<Tag> parameters)
		{
			if (parameters == null || parameters.Count == 0)
				return "";

			var paramStrings = new List<string>();
			foreach (var param in parameters)
			{
				// For now, treat all as strings (no +string suffix)
				// Later we can extend this to detect types: PARAM+integer=value, PARAM+boolean=value
				string encodedValue = UnityWebRequest.EscapeURL(param.Value);
				paramStrings.Add($"{param.Name}={encodedValue}");
			}

			return "&" + string.Join("&", paramStrings);
		}

		protected string ParseHyperBeamResponse(string response, bool wasSerialized)
		{
			if (wasSerialized)
			{
				// Parse: {"ao-result":"body","body":"{data}","device":"json@1.0"}
				try
				{
					JSONNode responseNode = JSON.Parse(response);
					if (responseNode.HasKey("body"))
					{
						return responseNode["body"];
					}
				}
				catch (System.Exception e)
				{
					if (showLogs) Debug.LogError($"[{gameObject.name}] Failed to parse serialized HyperBEAM response: {e.Message}");
					return response; // Return raw response as fallback
				}
			}

			// Return response as-is for non-serialized
			return response;
		}

		protected virtual IEnumerator SendHyperBeamMessage(string pid, string data, List<Tag> tags, Action<bool, NodeCU> callback, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			string ownerId;

			if (useMainWallet || string.IsNullOrEmpty(AOConnectManager.main.CurrentSessionAddress))
			{
				ownerId = AOConnectManager.main.CurrentAddress;
			}
			else
			{
				ownerId = AOConnectManager.main.CurrentSessionAddress;
			}

			if (data == null)
			{
				data = "";
			}

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending HyperBEAM message from {ownerId} to {pid} with data: {data}");
			}

			if (string.IsNullOrEmpty(ownerId))
			{
				Debug.LogError("Current address is null!!");
				var jsonResponse = new NodeCU("{\"Error\":\"Current address is null!\"}");
				callback?.Invoke(false, jsonResponse);
				yield break;
			}

			requestsCount++;
			string uniqueID = requestsCount.ToString();

			results[uniqueID] = (false, string.Empty, ownerId);

			JSONArray tagsJsonArray = new JSONArray();
			foreach (var tag in tags)
			{
				tagsJsonArray.Add(tag.ToJson());
			}

			if (AOConnectManager.main.addClientVersionTag && !string.IsNullOrEmpty(AOConnectManager.main.clientVersion))
			{
				tagsJsonArray.Add(new Tag("ClientVersion", AOConnectManager.main.clientVersion).ToJson());
			}

			string tagsStr = tagsJsonArray.ToString();

			AOConnectManager.main.SendMessageToProcessHyperBeam(pid, data, tagsStr, uniqueID, HyperBeamUrl, gameObject.name, "MessageCallback", useMainWallet, walletType);

			yield return new WaitUntil(() => results[uniqueID].Item1);

			var (result, response, savedAddress) = results[uniqueID];

			NodeCU networkResponse = new NodeCU(response);

			string currentOwnerId;

			if (useMainWallet || string.IsNullOrEmpty(AOConnectManager.main.CurrentSessionAddress))
			{
				currentOwnerId = AOConnectManager.main.CurrentAddress;
			}
			else
			{
				currentOwnerId = AOConnectManager.main.CurrentSessionAddress;
			}

			if (savedAddress != currentOwnerId)
			{
				if (showLogs)
				{
					Debug.LogError("Address mismatch between request and response.");
				}

				networkResponse = new NodeCU("{\"Error\":\"Address mismatch between request and response.\"}");

				if (resendIfResultFalse)
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], data, method: NetworkMethod.HyperBeamMessage, useMainWallet: useMainWallet);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(false, networkResponse);
					resendIndex = 0;
				}
			}
			else
			{
				if (!networkResponse.IsSuccessful() && resendIfResultFalse)
				{
					SendRequest(pid, tags, callback, delay: resendDelays[resendIndex], data, method: NetworkMethod.HyperBeamMessage, useMainWallet: useMainWallet);

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(networkResponse.IsSuccessful(), networkResponse);
					resendIndex = 0;
				}
			}

			results.Remove(uniqueID);
		}

		public virtual void ForceStopAndReset()
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] ForceStopAndReset called - stopping all coroutines and resetting state");
			}

			// Stop all coroutines running on this component
			StopAllCoroutines();

			// Reset retry state
			resendIndex = 0;
			elapsedTimeSinceFirstErrorMessage = 0;

			// Clear any pending results
			results.Clear();

			// Reset request counter (optional, but helps with debugging)
			requestsCount = 0;
		}
	}
}