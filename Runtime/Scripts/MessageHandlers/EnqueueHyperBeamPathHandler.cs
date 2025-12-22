using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
    /// <summary>
    /// Enqueue-based HyperBeam path handler that inherits from HyperBeamPathHandler
    /// Provides enqueue functionality for path requests
    /// </summary>
    public class EnqueueHyperBeamPathHandler : HyperBeamPathHandler
    {
        [Header("Enqueue Settings")]
        [Tooltip("Interval between HyperBeam path requests")]
        public float enqueueRequestInterval = 5f;
        [Tooltip("Maximum number of requests to keep in memory for processing")]
        public int maxQueueSize = 1;

        public bool callbackIfNotProcessed = false;

		private Queue<PathRequest> requestQueue = new Queue<PathRequest>();
		private bool isProcessing = false;
		protected float lastRequestTime = 0f;

        /// <summary>
        /// Structure to hold HyperBeam path request information
        /// </summary>
        protected struct PathRequest
        {
            public string pid;
            public string methodName;
            public List<Tag> tags;
            public Action<bool, string> callback;
            public bool serialize;
            public string moduleId;
            public CancellationToken cancellationToken;
        }

		protected PathRequest? currentRequest;


        /// <summary>
        /// Enqueue a HyperBeam dynamic request for processing
        /// </summary>
        public virtual void EnqueueHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback = null, bool serialize = true, string moduleId = null, CancellationToken cancellationToken = default)
        {
            // Use shared cancellation token if none provided
            if (cancellationToken == default)
            {
                cancellationToken = GetSharedCancellationToken();
            }

            float timeSinceLastRequest = Time.time - lastRequestTime;

            if (timeSinceLastRequest < enqueueRequestInterval && maxQueueSize <= requestQueue.Count)
            {
                if (showLogs) Debug.LogWarning($"[{gameObject.name}] Request ignored due to interval limit");
                if (callbackIfNotProcessed) callback?.Invoke(false, "Request ignored due to interval limit");
                return;
            }

            var queuedRequest = new PathRequest
            {
                pid = pid,
                methodName = methodName,
                tags = tags,
                callback = callback,
                serialize = serialize,
                moduleId = moduleId,
                cancellationToken = cancellationToken
            };

            // Add to queue (remove oldest if at capacity)
            if (requestQueue.Count >= maxQueueSize)
            {
                var removedRequest = requestQueue.Dequeue();
                if (showLogs) Debug.LogWarning($"[{gameObject.name}] Queue full, removing oldest request");
                if (callbackIfNotProcessed) removedRequest.callback?.Invoke(false, "Request removed due to queue overflow");
            }

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
            base.ForceStopAndReset();
            
            // Clear the request queue and notify callbacks
            while (requestQueue.Count > 0)
            {
                var request = requestQueue.Dequeue();
                if (callbackIfNotProcessed) request.callback?.Invoke(false, "Operation cancelled");
            }
            
            isProcessing = false;
            lastRequestTime = 0f;
            
            if (showLogs)
                Debug.Log("[EnqueueHyperBeamPathHandler] Queue cleared and state reset");
        }

        /// <summary>
        /// Process all queued requests sequentially with interval timing
        /// </summary>
        private async UniTask ProcessQueueAsync()
        {
            if (isProcessing) return;

            isProcessing = true;

            try
            {
                while (requestQueue.Count > 0)
                {
                    lastRequestTime = Time.time;
                    var request = requestQueue.Dequeue();

                    // Check if request was cancelled
                    if (request.cancellationToken.IsCancellationRequested)
                    {
                        request.callback?.Invoke(false, "Request cancelled");
                        continue;
                    }

                    try
                    {
                        // Send the request using the inherited method
                        (bool success, string result) = await SendDynamicRequestAsync(request.pid, request.methodName, request.tags, true, request.moduleId, request.serialize, callback:null, customIgnoreHttpErrors:null, cancellationToken:request.cancellationToken);

                        // Invoke callback
                        request.callback?.Invoke(success, result);

                        if (showLogs && success)
                        {
                            Debug.Log($"[{gameObject.name}] Enqueue request completed for {request.methodName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (showLogs) Debug.LogError($"[{gameObject.name}] Enqueue request failed: {ex.Message}");
                        request.callback?.Invoke(false, $"Request failed: {ex.Message}");
                    }

                    // Wait for interval before processing next request (if any)
                    if (requestQueue.Count > 0)
                    {
                        float remainingTime = enqueueRequestInterval - (Time.time - lastRequestTime);
                        if (remainingTime > 0)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken: request.cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            finally
            {
                isProcessing = false;
            }
        }

        /// <summary>
        /// Clear all pending requests in the queue
        /// </summary>
        public virtual void ClearQueue()
        {
            while (requestQueue.Count > 0)
            {
                var request = requestQueue.Dequeue();
                if (callbackIfNotProcessed) request.callback?.Invoke(false, "Queue cleared");
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
