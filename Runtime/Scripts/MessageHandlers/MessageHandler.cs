using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SimpleJSON;

namespace Permaverse.AO
{
	/// <summary>
	/// MessageHandler provides both callback-based and async methods for sending AO messages.
	/// 
	/// RECOMMENDED API:
	/// - Use SendRequestAsync() for new code - provides proper retry handling and returns results
	/// 
	/// LEGACY API (obsolete but still supported):
	/// - SendRequest() methods use callbacks only and have less predictable retry behavior
	/// - Marked as [Obsolete] to encourage migration to async methods
	/// 
	/// RETRY BEHAVIOR:
	/// - New async methods centralize retry logic with proper awaiting of all retry attempts
	/// - Callback parameter in async methods provides immediate feedback while retries continue
	/// - Final result is returned only after all retries complete or succeed
	/// </summary>
	public class MessageHandler : MonoBehaviour
	{
		[Header("ResendSettings")]
		[Tooltip("Maximum number of retry attempts for failed requests")]
		public int maxRetries = 5;
				
		public bool resendIfMissingMessages = true;
		public bool resendIfMissingTargetMessage = true;
		public int targetMessageIndex = 0;
		public bool resendIfTargetMessageNoData = true;
		public bool resendIfResultFalse = true;
		public List<int> resendDelays = new List<int> { 1, 5, 10, 30 };
		public float refreshAfterTimeInError = 10 * 60;
		public bool refreshEnabled = true;

		public bool doWeb2IfInEditor = false;

		private float elapsedTimeSinceFirstErrorMessage = 0;
		private bool hasRecentErrors = false;
		protected string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";

		[Header("HyperBEAM Settings")]
		[Tooltip("Override HyperBEAM URL. If empty, uses AOConnectManager.main.hyperBeamUrl")]
		public string hyperBeamUrlOverride = "";  // Empty = use AOConnectManager default

		// Property to get effective HyperBEAM URL
		protected string HyperBeamUrl =>
			!string.IsNullOrEmpty(hyperBeamUrlOverride) ? hyperBeamUrlOverride : AOConnectManager.main.hyperBeamUrl;

		protected int timeout = 120;

		protected Dictionary<string, (bool, string, string)> results = new Dictionary<string, (bool, string, string)>();
		protected int requestsCount = 0;

		protected float defaultDelay = 5f;

		// Single shared cancellation token source for ALL operations (simple StopAllCoroutines equivalent)
		private CancellationTokenSource _allOperationsCancellationTokenSource;
		protected bool showLogs => AOConnectManager.main.showLogs;


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
		}

		private void OnDestroy()
		{
			// Automatically cancel all operations when GameObject is destroyed
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
		}

		private void Update()
		{
			if (hasRecentErrors && refreshEnabled)
			{
				elapsedTimeSinceFirstErrorMessage += Time.deltaTime;
				if (elapsedTimeSinceFirstErrorMessage > refreshAfterTimeInError)
				{
					elapsedTimeSinceFirstErrorMessage = 0;
					hasRecentErrors = false;
					AOConnectManager.main.RefreshWebPage();
				}
			}
			else if (!hasRecentErrors)
			{
				elapsedTimeSinceFirstErrorMessage = 0;
			}
		}

		/// <summary>
		/// Send request and return result directly (async version with proper retry handling)
		/// </summary>
		/// <param name="pid">Process ID</param>
		/// <param name="tags">Tags to send</param>
		/// <param name="callback">Optional callback called once with final result</param>
		/// <param name="data">Optional data</param>
		/// <param name="method">Network method</param>
		/// <param name="useMainWallet">Use main wallet</param>
		/// <param name="walletType">Wallet type</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Tuple with success status and result</returns>
		public virtual async UniTask<(bool success, NodeCU result)> SendRequestAsync(string pid, List<Tag> tags, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, Action<bool, NodeCU> callback = null, CancellationToken cancellationToken = default)
		{
			for (int attempt = 0; attempt <= maxRetries; attempt++)
			{
				try
				{
					// Try the request
					(bool success, NodeCU result) = await SendRequestOnceAsync(pid, tags, data, method, useMainWallet, walletType, cancellationToken);
					
					// If successful, call callback and return immediately
					if (success)
					{
						hasRecentErrors = false; // Reset error tracking on success
						callback?.Invoke(true, result);
						return (true, result);
					}

					// If we don't retry on failure, call callback and return immediately
					if (!resendIfResultFalse)
					{
						callback?.Invoke(false, result);
						return (false, result);
					}

					// If this was the last attempt, return failure
					if (attempt == maxRetries)
					{
						hasRecentErrors = true; // Track that we had repeated failures
						callback?.Invoke(false, result);
						return (false, result);
					}

					// Calculate delay for next retry - use last delay if we run out of delays
					float delay = attempt < resendDelays.Count ? resendDelays[attempt] : (resendDelays.Count > 0 ? resendDelays[resendDelays.Count - 1] : defaultDelay);
					if (showLogs) Debug.Log($"[{gameObject.name}] Retrying request in {delay} seconds (attempt {attempt + 2})");
					
					hasRecentErrors = true; // Track that we're having errors requiring retries
					await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
				}
				catch (OperationCanceledException)
				{
					// Request was cancelled during retry delay
					callback?.Invoke(false, null);
					return (false, null);
				}
				catch (Exception ex)
				{
					if (showLogs) Debug.LogError($"[{gameObject.name}] Request failed: {ex.Message}");
					if (attempt == maxRetries)
					{
						callback?.Invoke(false, null);
						return (false, null);
					}
				}
			}

			return (false, null);
		}

		/// <summary>
		/// Send delayed request and return result directly (async version)
		/// </summary>
		public virtual async UniTask<(bool success, NodeCU result)> SendRequestDelayedAsync(string pid, List<Tag> tags, float delay, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, Action<bool, NodeCU> callback = null, CancellationToken cancellationToken = default)
		{
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException)
			{
				callback?.Invoke(false, null);
				return (false, null);
			}
			
			return await SendRequestAsync(pid, tags, data, method, useMainWallet, walletType, callback, cancellationToken);
		}

		/// <summary>
		/// Helper method to get owner ID based on wallet type and main wallet preference
		/// </summary>
		/// <param name="useMainWallet">Whether to use main wallet</param>
		/// <param name="walletType">Wallet type to use</param>
		/// <returns>Owner ID, or null if address info not found</returns>
		private string GetOwnerId(bool useMainWallet, WalletType walletType)
		{
			AddressInfo addressInfo = AOConnectManager.main.GetSecondaryWalletInfo(walletType);

			if (addressInfo == null)
			{
				Debug.LogError($"No address info found for wallet type: {walletType}");
				return null;
			}

			if (useMainWallet || string.IsNullOrEmpty(addressInfo.sessionKeyInfo?.address))
			{
				return addressInfo.address ?? "1234"; // HTTP method has this fallback
			}
			else
			{
				return addressInfo.sessionKeyInfo.address;
			}
		}

		/// <summary>
		/// Send a single request attempt without retry logic
		/// </summary>
		private async UniTask<(bool success, NodeCU result)> SendRequestOnceAsync(string pid, List<Tag> tags, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			if (method == NetworkMethod.Dryrun)
			{
				return await SendHttpPostRequestAsync(pid, tags, data, useMainWallet, walletType, cancellationToken);
			}
			else if (method == NetworkMethod.HyperBeamMessage)
			{
				return await SendHyperBeamMessageAsync(pid, data, tags, useMainWallet, walletType, cancellationToken);
			}
			else if (doWeb2IfInEditor && Application.isEditor)
			{
				return await SendHttpPostRequestAsync(pid, tags, data, useMainWallet, walletType, cancellationToken);
			}
			else
			{
				return await SendMessageToProcessAsync(pid, data, tags, useMainWallet, walletType, cancellationToken);
			}
		}

		/// <summary>
		/// Send HTTP POST request - single attempt without retry logic
		/// </summary>
		protected virtual async UniTask<(bool success, NodeCU result)> SendHttpPostRequestAsync(string pid, List<Tag> tags, string data = "", bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			string url = baseUrl + pid;

			string ownerId = GetOwnerId(useMainWallet, walletType);
			if (ownerId == null)
			{
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				return (false, errorResponse);
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
			catch (OperationCanceledException)
			{
				// Request was cancelled - don't retry, exit immediately
				if (showLogs) Debug.Log($"[{gameObject.name}] HTTP Post request cancelled");
				var cancelledResponse = new NodeCU("{\"Error\":\"Request cancelled\"}");
				return (false, cancelledResponse);
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
				return (false, jsonResponse);
			}
			else
			{
				jsonResponse = new NodeCU(request.downloadHandler.text);

				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] HTTP Result : {request.downloadHandler.text}");
				}

				// Check ShouldResend but don't retry - just return the result
				bool shouldRetry = ShouldResend(jsonResponse);
				return (!shouldRetry, jsonResponse);
			}
		}

		/// <summary>
		/// Send message to process - single attempt without retry logic
		/// </summary>
		protected virtual async UniTask<(bool success, NodeCU result)> SendMessageToProcessAsync(string pid, string data, List<Tag> tags, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			string ownerId = GetOwnerId(useMainWallet, walletType);
			if (ownerId == null)
			{
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				return (false, errorResponse);
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
				return (false, jsonResponse);
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

			// Check if the address changed during the request using GetOwnerId for consistency
			string currentOwnerId = GetOwnerId(useMainWallet, walletType);
			if (savedAddress != currentOwnerId)
			{
				if (showLogs) Debug.LogWarning($"[{gameObject.name}] Address changed during request. Expected: {currentOwnerId}, Saved: {savedAddress}");
			}

			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] SendMessageToProcess Result for {currentOwnerId}: {response}");
			}

			return (networkResponse.IsSuccessful(), networkResponse);
		}

		/// <summary>
		/// Send HyperBeam message - single attempt without retry logic
		/// </summary>
		protected virtual async UniTask<(bool success, NodeCU result)> SendHyperBeamMessageAsync(string pid, string data, List<Tag> tags, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			string ownerId = GetOwnerId(useMainWallet, walletType);
			if (ownerId == null)
			{
				var errorResponse = new NodeCU("{\"Error\":\"No wallet info found for specified type\"}");
				return (false, errorResponse);
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
				return (false, jsonResponse);
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

			// Check if the address changed during the request using GetOwnerId for consistency
			string currentOwnerId = GetOwnerId(useMainWallet, walletType);
			if (savedAddress != currentOwnerId)
			{
				if (showLogs)
				{
					Debug.LogError("Address mismatch between request and response.");
				}

				networkResponse = new NodeCU("{\"Error\":\"Address mismatch between request and response.\"}");
				return (false, networkResponse);
			}
			else
			{
				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] SendHyperBeamMessage Result for {currentOwnerId}: {response}");
				}

				return (networkResponse.IsSuccessful(), networkResponse);
			}
		}

		/// <summary>
		/// Legacy internal method for compatibility - just calls the new async method without retries
		/// </summary>
		[Obsolete("Internal method used by legacy retry logic - will be removed")]
		protected virtual async UniTask<NodeCU> SendRequestDelayedInternalAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, float delay, string data = "", NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default, CancellationToken cancellationToken = default)
		{
			// Wait for the delay first
			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException)
			{
				callback?.Invoke(false, null);
				return null;
			}

			// Then make a single request attempt without retries
			var (success, result) = await SendRequestOnceAsync(pid, tags, data, method, useMainWallet, walletType, cancellationToken);
			callback?.Invoke(success, result);
			return success ? result : null;
		}

		public void MessageCallback(string jsonResult)
		{
			if (showLogs)
			{
				Debug.Log($"[{gameObject.name}] Result: {jsonResult}");
			}

			// Check for null or empty result
			if (string.IsNullOrEmpty(jsonResult))
			{
				Debug.LogError($"[{gameObject.name}] MessageCallback received null or empty result");
				return;
			}

			try
			{
				var result = JSON.Parse(jsonResult);
				
				// Check if uniqueID exists
				if (result["uniqueID"] == null)
				{
					Debug.LogError($"[{gameObject.name}] MessageCallback received result without uniqueID: {jsonResult}");
					return;
				}
				
				string uniqueID = result["uniqueID"];

				if (results.ContainsKey(uniqueID))
				{
					results[uniqueID] = (true, jsonResult, results[uniqueID].Item3);
				}
				else
				{
					Debug.LogWarning($"[{gameObject.name}] MessageCallback received result for unknown uniqueID: {uniqueID}");
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[{gameObject.name}] MessageCallback error parsing result: {ex.Message}\nResult: {jsonResult}");
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
			hasRecentErrors = false;
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
	}
}