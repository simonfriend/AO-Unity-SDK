using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

		// Single shared cancellation token source for ALL operations (simple StopAllCoroutines equivalent)
		private CancellationTokenSource _allOperationsCancellationTokenSource;

		[Serializable]
		public enum NetworkMethod
		{
			Dryrun,
			Message,
			HyperBeamMessage
		}

		private void Start()
		{
			// Initialize the single shared cancellation token source
			_allOperationsCancellationTokenSource = new CancellationTokenSource();

			if (!Application.isEditor)
			{
				showLogs = UrlUtilities.GetUrlParameterValue("showLogs") == "true";
			}
			else
			{
				showLogs = true;
			}
		}

		private void OnDestroy()
		{
			// Automatically cancel all operations when GameObject is destroyed
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
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
			SendRequestAsync(pid, tags, callback, data, method, useMainWallet, walletType, this.GetCancellationTokenOnDestroy()).Forget();
		}

		public virtual void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, float delay, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			SendRequestDelayedAsync(pid, tags, callback, delay, data, method, useMainWallet, walletType, GetSharedCancellationToken()).Forget();
		}

		public virtual void SendHyperBeamStaticRequest(string pid, string cachePath, Action<bool, string> callback, bool now = true, bool serialize = true, bool addCachePath = true)
		{
			string path = BuildHyperBeamStaticPath(pid, cachePath, now, serialize, addCachePath);
			SendHyperBeamPathAsync(path, callback, GetSharedCancellationToken()).Forget();
		}

		public virtual void SendHyperBeamDynamicRequest(string pid, string methodName, List<Tag> parameters, Action<bool, string> callback, bool now = true, bool serialize = true, string moduleId = null)
		{
			string path = BuildHyperBeamDynamicPath(pid, methodName, parameters, now, serialize, moduleId);
			SendHyperBeamPathAsync(path, callback, GetSharedCancellationToken()).Forget();
		}

		// UniTask versions - Zero allocation async methods
		protected virtual async UniTask SendRequestAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			if (method == NetworkMethod.Dryrun)
			{
				await SendHttpPostRequestAsync(pid, tags, callback, data, useMainWallet, walletType, cancellationToken);
			}
			else if (method == NetworkMethod.HyperBeamMessage)
			{
				await SendHyperBeamMessageAsync(pid, data, tags, callback, useMainWallet, walletType, cancellationToken);
			}
			else if (doWeb2IfInEditor && Application.isEditor)
			{
				await SendHttpPostRequestAsync(pid, tags, callback, data, useMainWallet, walletType, cancellationToken);
			}
			else
			{
				await SendMessageToProcessAsync(pid, data, tags, callback, useMainWallet, walletType, cancellationToken);
			}
		}

		protected virtual async UniTask SendRequestDelayedAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, float delay, string data = "", NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
			await SendRequestAsync(pid, tags, callback, data, method, useMainWallet, walletType, cancellationToken);
		}

		protected virtual async UniTask SendHyperBeamPathAsync(string url, Action<bool, string> callback, CancellationToken cancellationToken = default)
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending HyperBEAM path request to: {url}");
			}

			using UnityWebRequest request = UnityWebRequest.Get(url);
			request.timeout = timeout;

			try
			{
				await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
			}
			catch (UnityWebRequestException ex)
			{
				// UniTask throws UnityWebRequestException for HTTP errors, but we want to handle them as normal flow
				if (showLogs) Debug.LogError($"[{gameObject.name}] HyperBEAM path Error: {ex.UnityWebRequest.error}");
			}

			if (request.result != UnityWebRequest.Result.Success)
			{
				// Use retry logic before fallback
				if (resendIfResultFalse)
				{
					if (showLogs) Debug.Log($"[{gameObject.name}] Retrying HyperBEAM path request in {resendDelays[resendIndex]} seconds");

					// Fire and forget - don't await, just like the original StartCoroutine
					RetryHyperBeamPathRequestDelayedAsync(url, callback, resendDelays[resendIndex], GetSharedCancellationToken()).Forget();

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
		
		protected virtual async UniTask RetryHyperBeamPathRequestDelayedAsync(string url, Action<bool, string> callback, float delay, CancellationToken cancellationToken = default)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
			await SendHyperBeamPathAsync(url, callback, cancellationToken);
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

		public virtual void ForceStopAndReset()
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] ForceStopAndReset called - stopping ALL operations and resetting state");
			}

			// Cancel ALL running operations immediately (UniTask equivalent of StopAllCoroutines)
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();

			// Create fresh cancellation token source for new operations
			_allOperationsCancellationTokenSource = new CancellationTokenSource();

			// Reset retry state
			resendIndex = 0;
			elapsedTimeSinceFirstErrorMessage = 0;

			// Clear any pending results
			results.Clear();

			// Reset request counter
			requestsCount = 0;
		}

		// Simple helper to get the shared cancellation token
		protected CancellationToken GetSharedCancellationToken()
		{
			// Ensure we have a valid token source
			if (_allOperationsCancellationTokenSource == null || _allOperationsCancellationTokenSource.IsCancellationRequested)
			{
				_allOperationsCancellationTokenSource = new CancellationTokenSource();
			}

			return _allOperationsCancellationTokenSource.Token;
		}

		// UniTask async methods for zero-allocation networking
		protected virtual async UniTask SendHttpPostRequestAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = "", bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			string url = baseUrl + pid;

			// Get the appropriate AddressInfo based on walletType
			AddressInfo addressInfo = AOConnectManager.main.GetSecondaryWalletInfo(walletType);

			if (addressInfo == null)
			{
				Debug.LogError($"No address info found for wallet type: {walletType}");
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				callback?.Invoke(false, errorResponse);
				return;
			}

			string ownerId;
			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				ownerId = addressInfo.address ?? "1234";
			}
			else
			{
				ownerId = addressInfo.sessionKeyInfo.address;
			}

			string jsonBody = CreateJsonBody(pid, ownerId, tags, data);

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Sending request | {jsonBody}");
			}

			using UnityWebRequest request = new UnityWebRequest(url, "POST");
			byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.timeout = timeout;
			request.SetRequestHeader("Content-Type", "application/json");

			try
			{
				await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
			}
			catch (UnityWebRequestException ex)
			{
				// UniTask throws UnityWebRequestException for HTTP errors, but we want to handle them as normal flow
				if (showLogs) Debug.LogError($"[{gameObject.name}] HTTP Post Error: {ex.UnityWebRequest.error} | {jsonBody}");
			}

			NodeCU jsonResponse;

			if (request.result != UnityWebRequest.Result.Success)
			{
				jsonResponse = new NodeCU($"{{\"Error\":\"{request.error}\"}}");

				if (resendIfResultFalse)
				{
					// Fire and forget - don't await, maintain original behavior with delay
					SendRequestDelayedAsync(pid, tags, callback, resendDelays[resendIndex], data, NetworkMethod.Dryrun, useMainWallet, walletType, GetSharedCancellationToken()).Forget();

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
					// Fire and forget - don't await, maintain original behavior with delay  
					SendRequestDelayedAsync(pid, tags, callback, resendDelays[resendIndex], data, NetworkMethod.Dryrun, useMainWallet, walletType, GetSharedCancellationToken()).Forget();

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

		protected virtual async UniTask SendMessageToProcessAsync(string pid, string data, List<Tag> tags, Action<bool, NodeCU> callback, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			// Get the appropriate AddressInfo based on walletType
			AddressInfo addressInfo = AOConnectManager.main.GetSecondaryWalletInfo(walletType);

			if (addressInfo == null)
			{
				Debug.LogError($"No address info found for wallet type: {walletType}");
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				callback?.Invoke(false, errorResponse);
				return;
			}

			string ownerId;
			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				ownerId = addressInfo.address;
			}
			else
			{
				ownerId = addressInfo.sessionKeyInfo.address;
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
				return;
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

			// Use UniTask WaitUntil instead of coroutine
			await UniTask.WaitUntil(() => results[uniqueID].Item1, cancellationToken: cancellationToken);

			var (result, response, savedAddress) = results[uniqueID];

			NodeCU networkResponse = new NodeCU(response);

			// Use the same logic as for initial ownerId to determine what address should be expected
			string currentOwnerId;
			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				currentOwnerId = addressInfo.address;
			}
			else
			{
				currentOwnerId = addressInfo.sessionKeyInfo.address;
			}

			// Check if the address changed during the request
			if (savedAddress != currentOwnerId)
			{
				if (showLogs) Debug.LogWarning($"[{gameObject.name}] Address changed during request. Expected: {currentOwnerId}, Saved: {savedAddress}");
			}

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] SendMessageToProcess Result for {currentOwnerId}: {response}");
			}

			if (networkResponse.IsSuccessful())
			{
				callback?.Invoke(true, networkResponse);
				resendIndex = 0;
			}
			else
			{
				if (resendIfResultFalse)
				{
					// Fire and forget - don't await, maintain original behavior with delay
					SendRequestDelayedAsync(pid, tags, callback, resendDelays[resendIndex], data, NetworkMethod.Message, useMainWallet, walletType, GetSharedCancellationToken()).Forget();

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

		protected virtual async UniTask SendHyperBeamMessageAsync(string pid, string data, List<Tag> tags, Action<bool, NodeCU> callback, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			// Get the appropriate AddressInfo based on walletType
			AddressInfo addressInfo = AOConnectManager.main.GetSecondaryWalletInfo(walletType);

			if (addressInfo == null)
			{
				Debug.LogError($"No address info found for wallet type: {walletType}");
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				callback?.Invoke(false, errorResponse);
				return;
			}

			string ownerId;
			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				ownerId = addressInfo.address;
			}
			else
			{
				ownerId = addressInfo.sessionKeyInfo.address;
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
				return;
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

			// Use UniTask WaitUntil instead of coroutine
			await UniTask.WaitUntil(() => results[uniqueID].Item1, cancellationToken: cancellationToken);

			var (result, response, savedAddress) = results[uniqueID];

			NodeCU networkResponse = new NodeCU(response);

			// Use the same logic as for initial ownerId to determine what address should be expected
			string currentOwnerId;
			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				currentOwnerId = addressInfo.address;
			}
			else
			{
				currentOwnerId = addressInfo.sessionKeyInfo.address;
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
					// Fire and forget - don't await, maintain original behavior with delay
					SendRequestDelayedAsync(pid, tags, callback, resendDelays[resendIndex], data, NetworkMethod.HyperBeamMessage, useMainWallet, walletType, GetSharedCancellationToken()).Forget();

					if (increaseResendDelay && resendIndex + 1 < resendDelays.Count)
					{
						resendIndex++;
					}
				}
				else
				{
					callback?.Invoke(false, networkResponse);
				}
			}
			else
			{
				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] SendHyperBeamMessage Result for {currentOwnerId}: {response}");
				}

				if (networkResponse.IsSuccessful())
				{
					callback?.Invoke(true, networkResponse);
					resendIndex = 0;
				}
				else
				{
					if (resendIfResultFalse)
					{
						// Fire and forget - don't await, maintain original behavior with delay
						SendRequestDelayedAsync(pid, tags, callback, resendDelays[resendIndex], data, NetworkMethod.HyperBeamMessage, useMainWallet, walletType, GetSharedCancellationToken()).Forget();

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
			}

			results.Remove(uniqueID);
		}
	}
}