using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
	public class EnqueueMessageHandler : MessageHandler
	{
		[Header("Enqueue Settings")]
		[Tooltip("Interval between message requests")]
		public float enqueueRequestInterval = 5f;
		[Tooltip("Maximum number of messages to keep in memory for processing")]
		public int maxQueueSize = 1;

		public bool callbackIfNotProcessed = false;

		private Queue<MessageRequest> requestQueue = new Queue<MessageRequest>();
		private bool isProcessing = false;
		protected float lastRequestTime = 0f;

		/// <summary>
		/// Structure to hold message request information
		/// </summary>
		protected struct MessageRequest
		{
			public string pid;
			public List<Tag> tags;
			public Action<bool, NodeCU> callback;
			public string data;
			public NetworkMethod method;
			public bool useMainWallet;
			public WalletType walletType;
			public CancellationToken cancellationToken;
		}

		protected MessageRequest? currentRequest;

		// UniTask versions for zero-allocation performance
		public virtual void EnqueueRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			// Use shared cancellation token if none provided
			var cancellationToken = GetSharedCancellationToken();
			
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && maxQueueSize <= requestQueue.Count)
			{
				if(callbackIfNotProcessed) 
				{
					var errorResponse = new NodeCU("{\"Error\":\"Request rejected due to interval limit\"}");
					callback?.Invoke(false, errorResponse);
				}
				return;
			}

			var queuedRequest = new MessageRequest
			{
				pid = pid,
				tags = tags,
				callback = callback,
				data = data,
				method = method,
				useMainWallet = useMainWallet,
				walletType = walletType,
				cancellationToken = cancellationToken
			};

			requestQueue.Enqueue(queuedRequest);

			// Start processing if not already running
			if (!isProcessing)
			{
				ProcessQueueAsync().Forget();
			}
		}

		/// <summary>
		/// Force stop all running operations and reset state
		/// Override to also clear the request queue
		/// </summary>
		public override void ForceStopAndReset()
		{
			// Call base class implementation first (handles shared cancellation token)
			base.ForceStopAndReset();

			// Clear the request queue and notify callbacks
			while (requestQueue.Count > 0)
			{
				var request = requestQueue.Dequeue();
				if(callbackIfNotProcessed) 
				{
					var errorResponse = new NodeCU("{\"Error\":\"Operation cancelled\"}");
					request.callback?.Invoke(false, errorResponse);
				}
			}

			requestQueue.Clear();

			// Reset processing state
			isProcessing = false;
			lastRequestTime = 0f;
			
			if (showLogs)
				Debug.Log("[EnqueueMessageHandler] Queue cleared and state reset");
		}

		/// <summary>
		/// Process the request queue with proper timing intervals
		/// </summary>
		protected virtual async UniTask ProcessQueueAsync(CancellationToken cancellationToken = default)
		{
			isProcessing = true;

			try
			{
				while (requestQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
				{
					// Get the next request from queue
					var request = requestQueue.Dequeue();
					currentRequest = request;
					
					lastRequestTime = Time.time;
					
					if (showLogs) Debug.Log($"[{gameObject.name}] Processing request for PID: {request.pid}");

					// Send the request using the centralized retry logic
					var (success, result) = await SendRequestAsync(
						request.pid, 
						request.tags, 
						request.data, 
						request.method, 
						request.useMainWallet, 
						request.walletType, 
						callback: null,
						request.cancellationToken
					);

					// Call the callback with the final result
					request.callback?.Invoke(success, result);

					// Wait for the remaining interval time before processing next request
					float remainingTime = enqueueRequestInterval - (Time.time - lastRequestTime);
					if (remainingTime > 0 && !cancellationToken.IsCancellationRequested)
					{
						await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken: cancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested
				if (showLogs) Debug.Log($"[{gameObject.name}] Queue processing cancelled");
				
			}
			finally
			{
				isProcessing = false;
				currentRequest = null;

				if (showLogs) Debug.Log($"[{gameObject.name}] Queue processing finished");
			}
		}

		/// <summary>
		/// Clear the request queue and notify all pending callbacks
		/// </summary>
		public virtual void ClearQueue()
		{
			while (requestQueue.Count > 0)
			{
				var request = requestQueue.Dequeue();
				if(callbackIfNotProcessed) 
				{
					var errorResponse = new NodeCU("{\"Error\":\"Queue cleared\"}");
					request.callback?.Invoke(false, errorResponse);
				}
			}

			if (showLogs) Debug.Log($"[{gameObject.name}] Request queue cleared");
		}

		/// <summary>
		/// Get current queue size
		/// </summary>
		public int GetQueueSize()
		{
			return requestQueue.Count;
		}

		/// <summary>
		/// Check if queue is currently being processed
		/// </summary>
		public bool IsProcessing()
		{
			return isProcessing;
		}
	}
}