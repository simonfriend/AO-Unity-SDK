using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
	/// <summary>
	/// Dedicated handler for HyperBEAM path requests with composable path building
	/// Provides fluent API and preset helpers for common operations
	/// </summary>
	public class HyperBeamPathHandler : MonoBehaviour
	{
		[Header("HyperBEAM Configuration")]
		[Tooltip("Override HyperBEAM URL. If empty, uses AOConnectManager.main.hyperBeamUrl")]
		public string hyperBeamUrlOverride = "";  // Empty = use AOConnectManager default

		[Header("Request Settings")]
		[Tooltip("Whether to retry failed requests")]
		public bool resendIfResultFalse = true;

		[Tooltip("Maximum number of retry attempts for failed requests")]
		public int maxRetries = 5;

		[Tooltip("Delay between retry attempts")]
		public List<int> resendDelays = new List<int> { 1, 5, 10, 30 };

		protected float defaultDelay = 5f;

		protected bool showLogs => AOConnectManager.main.showLogs;

		// Shared cancellation token source for ForceStopAndReset functionality
		protected CancellationTokenSource sharedCancellationTokenSource;

		/// <summary>
		/// Get the HyperBEAM URL to use (override or default)
		/// </summary>
		protected string HyperBeamUrl =>
			!string.IsNullOrEmpty(hyperBeamUrlOverride) ? hyperBeamUrlOverride : AOConnectManager.main.hyperBeamUrl;

		/// <summary>
		/// Force stop all running operations and reset state
		/// </summary>
		public virtual void ForceStopAndReset()
		{
			if (sharedCancellationTokenSource != null)
			{
				sharedCancellationTokenSource.Cancel();
				sharedCancellationTokenSource.Dispose();
			}
			sharedCancellationTokenSource = new CancellationTokenSource();

			if (showLogs)
				Debug.Log("[HyperBeamPathHandler] Force stopped and reset all operations");
		}

		/// <summary>
		/// Get the shared cancellation token, creating a new token source if needed
		/// </summary>
		protected CancellationToken GetSharedCancellationToken()
		{
			// Ensure we have a valid token source
			if (sharedCancellationTokenSource == null || sharedCancellationTokenSource.IsCancellationRequested)
			{
				sharedCancellationTokenSource = new CancellationTokenSource();
			}

			return sharedCancellationTokenSource.Token;
		}

		protected virtual void Start()
		{
			// Initialize cancellation token source
			sharedCancellationTokenSource = new CancellationTokenSource();
		}

		protected virtual void OnDestroy()
		{
			// Clean up cancellation token source
			if (sharedCancellationTokenSource != null)
			{
				sharedCancellationTokenSource.Cancel();
				sharedCancellationTokenSource.Dispose();
			}
		}

		#region Core Path Request Methods

		/// <summary>
		/// Send HyperBeam path request with centralized retry logic
		/// </summary>
		/// <param name="url">URL to request</param>
		/// <param name="callback">Optional callback for final result only</param>
		/// <param name="serialize">Whether to serialize response</param>
		/// <param name="cancellationToken">Cancellation token (uses shared token if not provided)</param>
		/// <returns>Tuple with success status and result</returns>
		public virtual async UniTask<(bool success, string result)> SendPathAsync(string url, bool serialize = true, Action<bool, string> callback = null, CancellationToken cancellationToken = default)
		{
			// Use shared cancellation token if none provided
			if (cancellationToken == default)
			{
				cancellationToken = GetSharedCancellationToken();
			}

			for (int attempt = 0; attempt <= maxRetries; attempt++)
			{
				try
				{
					(bool success, string result) = await SendPathOnceAsync(url, serialize, cancellationToken);

					// If successful, call callback and return immediately
					if (success)
					{
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
						callback?.Invoke(false, result);
						return (false, result);
					}

					// Calculate delay for next retry - use last delay if we run out of delays
					float delay = attempt < resendDelays.Count ? resendDelays[attempt] : (resendDelays.Count > 0 ? resendDelays.Last() : defaultDelay);
					if (showLogs) Debug.Log($"[{gameObject.name}] Retrying HyperBEAM path request in {delay} seconds (attempt {attempt + 2})");

					await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
				}
				catch (OperationCanceledException)
				{
					// Request was cancelled during retry delay
					if (showLogs) Debug.Log($"[{gameObject.name}] HyperBEAM path request cancelled");
					callback?.Invoke(false, null);
					return (false, null);
				}
				catch (Exception ex)
				{
					if (showLogs) Debug.LogError($"[{gameObject.name}] HyperBEAM path request failed: {ex.Message}");
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
		/// Send a single HyperBeam path request attempt without retry logic
		/// </summary>
		private async UniTask<(bool success, string result)> SendPathOnceAsync(string url, bool serialize = true, CancellationToken cancellationToken = default)
		{
			if (showLogs) Debug.Log($"[{gameObject.name}] Sending HyperBEAM path request to: {url}");

			using var request = UnityWebRequest.Get(url);

			try
			{
				await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException)
			{
				// Re-throw cancellation - let the outer catch handle it
				if (showLogs) Debug.Log($"[{gameObject.name}] HyperBEAM path request cancelled");
				throw;
			}
			catch (Exception ex)
			{
				// Handle other exceptions during the request
				if (showLogs) Debug.LogError($"[{gameObject.name}] HyperBEAM path Exception: {ex.Message}");
			}

			if (request.result == UnityWebRequest.Result.Success)
			{
				string responseData = ParseHyperBeamResponse(request.downloadHandler.text, serialize);
				if (showLogs) Debug.Log($"[{gameObject.name}] HyperBEAM path Success: {responseData}");
				return (true, responseData);
			}
			else
			{
				if (showLogs) Debug.LogError($"[{gameObject.name}] HyperBEAM path Error: {request.error}");
				return (false, null);
			}
		}

		#endregion

		#region Path Building Methods

		/// <summary>
		/// Build HyperBEAM static path for cached content
		/// </summary>
		/// <param name="slot">Optional slot number for compute results. If set and now=false, uses "compute={slot}" instead of "compute"</param>
		public string BuildStaticPath(string pid, string path, bool now = false, bool addCachePath = false, bool serialize = true, long? slot = null)
		{
			// Determine compute mode: "now", "compute", or "compute={slot}"
			string computeMode;
			if (now)
			{
				computeMode = "now";
			}
			else if (slot.HasValue)
			{
				computeMode = $"compute={slot.Value}";
			}
			else
			{
				computeMode = "compute";
			}

			string baseUrl = $"{HyperBeamUrl}/{pid}~process@1.0/{computeMode}";
			string cachePrefix = addCachePath ? "/cache" : "";
			// string serializeSuffix = serialize ? "/serialize~json@1.0" : "";
			string serializeSuffix = serialize ? "?accept=application/json&accept-bundle=true" : "";


			return $"{baseUrl}{cachePrefix}/{path}{serializeSuffix}";
		}

		/// <summary>
		/// Build HyperBEAM dynamic path with parameters
		/// </summary>
		/// <param name="slot">Optional slot number for compute results. If set and now=false, uses "compute={slot}" instead of "compute"</param>
		public string BuildDynamicPath(string pid, string methodName, List<Tag> parameters, bool now = false, string moduleId = null, bool serialize = true, long? slot = null)
		{
			// Determine compute mode: "now", "compute", or "compute={slot}"
			string computeMode;
			if (now)
			{
				computeMode = "now";
			}
			else if (slot.HasValue)
			{
				computeMode = $"compute={slot.Value}";
			}
			else
			{
				computeMode = "compute";
			}

			string baseUrl = $"{HyperBeamUrl}/{pid}~process@1.0/{computeMode}";
			string paramString = BuildParameterString(parameters, moduleId);
			// string serializeSuffix = serialize ? "/serialize~json@1.0" : "";
			string serializeSuffix = serialize ? "?accept=application/json&accept-bundle=true" : "";
			return $"{baseUrl}/~lua@5.3a&module={moduleId ?? "default"}{paramString}/{methodName}{serializeSuffix}";
		}

		/// <summary>
		/// Build parameter string from tag list
		/// </summary>
		private string BuildParameterString(List<Tag> parameters, string moduleId = null)
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

		#endregion

		#region Response Parsing

		/// <summary>
		/// Parse HyperBEAM response based on serialization format
		/// </summary>
		protected string ParseHyperBeamResponse(string response, bool wasSerialized)
		{
			if (wasSerialized)
			{
				// With new query parameter approach (?accept=application/json&accept-bundle=true), HyperBEAM returns a bundle format:
				// {"ao-result":"body","body":{data},"commitments":{...},"status":200}
				try
				{
					var responseNode = SimpleJSON.JSON.Parse(response);
					if (responseNode.HasKey("body"))
					{
						var bodyNode = responseNode["body"];
						
						// If body is a string, return it as-is
						if (bodyNode.IsString)
						{
							return bodyNode.Value;
						}
						
						// If body is an object or array, serialize it back to JSON string
						return bodyNode.ToString();
					}
				}
				catch (Exception e)
				{
					if (showLogs) Debug.LogError($"[{gameObject.name}] Failed to parse serialized HyperBEAM response: {e.Message}");
					return response; // Return raw response as fallback
				}
			}

			// Return response as-is for non-serialized
			return response;
		}

		#endregion

		#region High-Level Request Methods

		/// <summary>
		/// Send static HyperBEAM request for cached content
		/// </summary>
		/// <param name="slot">Optional slot number for compute results. If set and now=false, uses "compute={slot}" instead of "compute"</param>
		public async UniTask<(bool success, string result)> SendStaticRequestAsync(string pid, string path, bool now = true, bool addCachePath = true, bool serialize = true, long? slot = null, Action<bool, string> callback = null, CancellationToken cancellationToken = default)
		{
			string fullPath = BuildStaticPath(pid, path, now, addCachePath, serialize, slot);
			return await SendPathAsync(fullPath, serialize: serialize, callback: callback, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Send dynamic HyperBEAM request with method and parameters
		/// </summary>
		/// <param name="slot">Optional slot number for compute results. If set and now=false, uses "compute={slot}" instead of "compute"</param>
		public async UniTask<(bool success, string result)> SendDynamicRequestAsync(string pid, string methodName, List<Tag> parameters, bool now = true, string moduleId = null, bool serialize = true, long? slot = null, Action<bool, string> callback = null, CancellationToken cancellationToken = default)
		{
			string fullPath = BuildDynamicPath(pid, methodName, parameters, now, moduleId, serialize, slot);
			var (success, result) = await SendPathAsync(fullPath, serialize: serialize, callback: callback, cancellationToken: cancellationToken);
			return (success, result);
		}

		#endregion

		#region Fluent Path Builder

		/// <summary>
		/// Start building a fluent path for the given process
		/// </summary>
		public PathBuilder ForProcess(string pid) => new PathBuilder(this, pid);

		/// <summary>
		/// Fluent builder for composable HyperBEAM paths
		/// </summary>
		public class PathBuilder
		{
			private readonly HyperBeamPathHandler handler;
			private readonly string pid;
			private readonly List<Tag> parameters = new List<Tag>();
			private string methodName = "";
			private string moduleId = null;
			private bool now = true;
			private bool serialize = true;
			private long? slot = null;

			internal PathBuilder(HyperBeamPathHandler handler, string pid)
			{
				this.handler = handler;
				this.pid = pid;
			}

			/// <summary>
			/// Set the method name for dynamic requests
			/// </summary>
			public PathBuilder Method(string methodName)
			{
				this.methodName = methodName;
				return this;
			}

			/// <summary>
			/// Add a parameter to the request
			/// </summary>
			public PathBuilder Parameter(string name, string value)
			{
				parameters.Add(new Tag(name, value));
				return this;
			}

			/// <summary>
			/// Add multiple parameters at once
			/// </summary>
			public PathBuilder Parameters(params (string name, string value)[] parameters)
			{
				foreach (var (name, value) in parameters)
				{
					this.parameters.Add(new Tag(name, value));
				}
				return this;
			}

			/// <summary>
			/// Set the Lua module ID
			/// </summary>
			public PathBuilder Module(string moduleId)
			{
				this.moduleId = moduleId;
				return this;
			}

			/// <summary>
			/// Set whether to use 'now' or 'compute' mode
			/// </summary>
			public PathBuilder Now(bool now = true)
			{
				this.now = now;
				return this;
			}

			/// <summary>
			/// Set the slot number for compute results
			/// </summary>
			public PathBuilder Slot(long slot)
			{
				this.slot = slot;
				return this;
			}

			/// <summary>
			/// Set whether to serialize the response
			/// </summary>
			public PathBuilder Serialize(bool serialize = true)
			{
				this.serialize = serialize;
				return this;
			}

			/// <summary>
			/// Build the URL string
			/// </summary>
			public string BuildUrl()
			{
				return handler.BuildDynamicPath(pid, methodName, parameters, now, moduleId, serialize, slot);
			}

			/// <summary>
			/// Execute the request and return tuple result
			/// </summary>
			public async UniTask<(bool success, string result)> ExecuteAsync(Action<bool, string> callback = null, CancellationToken cancellationToken = default)
			{
				return await handler.SendDynamicRequestAsync(pid, methodName, parameters, now, moduleId, serialize, slot, callback, cancellationToken);
			}
		}

		#endregion

	}
}
