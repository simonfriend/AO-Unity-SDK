using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
    /// <summary>
    /// Queue-based GraphQL handler for rate-limited processing
    /// Extends GraphQLHandler with request queuing and interval management
    /// </summary>
    public class EnqueueGraphQLHandler : GraphQLHandler
    {
        [Header("Queue Settings")]
        [Tooltip("Maximum number of concurrent requests")]
        public int maxConcurrentRequests = 1;
        
        [Tooltip("Interval between processing queue items")]
        public float enqueueRequestInterval = 1.0f; // Seconds between requests
        
        [Tooltip("Maximum queue size before dropping requests")]
        public int maxQueueSize = 1;
        
        [Tooltip("Whether to call callback for requests rejected due to interval limits")]
        public bool callbackIfNotProcessed = false;

        private Queue<GraphQLQueueItem> requestQueue = new Queue<GraphQLQueueItem>();
        private Queue<ProcessTransactionsQueueItem> processRequestQueue = new Queue<ProcessTransactionsQueueItem>();
        private int activeRequests = 0;
        private bool isProcessingQueue = false;

        protected struct GraphQLQueueItem
        {
            public string Query;
            public Action<bool, string> Callback;
            public List<string> Endpoints;
            public DateTime QueuedTime;
            public CancellationToken CancellationToken;
        }

        protected struct ProcessTransactionsQueueItem
        {
            public string ProcessId;
            public List<Tag> AdditionalTags;
            public Action<bool, Dictionary<string, Message>> Callback;
            public int First;
            public bool GetData;
            public List<string> Endpoints;
            public DateTime QueuedTime;
            public long? FromTimestamp;
            public CancellationToken CancellationToken;
        }

        /// <summary>
        /// Enqueue a GraphQL query
        /// </summary>
        public virtual void EnqueueGraphQLQuery(
            string query, 
            Action<bool, string> callback = null, 
            List<string> endpoints = null,
            CancellationToken cancellationToken = default)
        {
            // Use shared cancellation token if none provided
            if (cancellationToken == default)
            {
                cancellationToken = GetSharedCancellationToken();
            }

            if (requestQueue.Count >= maxQueueSize)
            {
                if (showLogs) 
                {
                    Debug.LogWarning($"[{gameObject.name}] GraphQL queue is full, dropping request");
                }
                if (callbackIfNotProcessed) callback?.Invoke(false, "{\"Error\":\"Queue is full\"}");
                return;
            }

            var queueItem = new GraphQLQueueItem
            {
                Query = query,
                Callback = callback,
                Endpoints = endpoints,
                QueuedTime = DateTime.Now,
                CancellationToken = cancellationToken
            };

            requestQueue.Enqueue(queueItem);

            if (showLogs)
            {
                Debug.Log($"[{gameObject.name}] GraphQL query queued. Queue size: {requestQueue.Count}");
            }

            // Start processing if not already running
            if (!isProcessingQueue)
            {
                StartQueueProcessing();
            }
        }

        /// <summary>
        /// Enqueue a process transactions query
        /// </summary>
        public virtual void EnqueueProcessTransactionsQuery(
            string processId,
            List<Tag> additionalTags = null,
            Action<bool, Dictionary<string, Message>> callback = null,
            int first = 1,
            bool getData = true,
            List<string> endpoints = null,
            CancellationToken cancellationToken = default,
            long? fromTimestamp = null)
        {
            // Use shared cancellation token if none provided
            if (cancellationToken == default)
            {
                cancellationToken = GetSharedCancellationToken();
            }

            if (processRequestQueue.Count >= maxQueueSize)
            {
                if (showLogs) 
                {
                    Debug.LogWarning($"[{gameObject.name}] Process transactions queue is full, dropping request");
                }
                if (callbackIfNotProcessed) callback?.Invoke(false, null);
                return;
            }

            var queueItem = new ProcessTransactionsQueueItem
            {
                ProcessId = processId,
                AdditionalTags = additionalTags,
                Callback = callback,
                First = first,
                GetData = getData,
                Endpoints = endpoints,
                QueuedTime = DateTime.Now,
                FromTimestamp = fromTimestamp,
                CancellationToken = cancellationToken
            };

            processRequestQueue.Enqueue(queueItem);

            if (showLogs)
            {
                Debug.Log($"[{gameObject.name}] Process transactions query queued. Queue size: {processRequestQueue.Count}");
            }

            // Start processing if not already running
            if (!isProcessingQueue)
            {
                StartQueueProcessing();
            }
        }

        /// <summary>
        /// Start processing the queue
        /// </summary>
        protected virtual void StartQueueProcessing()
        {
            if (!isProcessingQueue)
            {
                isProcessingQueue = true;
                ProcessQueueAsync(GetSharedCancellationToken()).Forget();
            }
        }

        /// <summary>
        /// Process the queue continuously
        /// </summary>
        protected virtual async UniTask ProcessQueueAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                while ((requestQueue.Count > 0 || processRequestQueue.Count > 0) && !cancellationToken.IsCancellationRequested)
                {
                    // Check if we can process more requests
                    if (activeRequests >= maxConcurrentRequests)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(enqueueRequestInterval), cancellationToken: cancellationToken);
                        continue;
                    }

                    // Process GraphQL queue
                    if (requestQueue.Count > 0)
                    {
                        var queueItem = requestQueue.Dequeue();
                        activeRequests++;
                        ProcessGraphQLRequestAsync(queueItem, queueItem.CancellationToken).Forget();
                    }
                    // Process process transactions queue
                    else if (processRequestQueue.Count > 0)
                    {
                        var queueItem = processRequestQueue.Dequeue();
                        activeRequests++;
                        ProcessProcessTransactionsRequestAsync(queueItem, queueItem.CancellationToken).Forget();
                    }

                    // Wait between processing items
                    if (requestQueue.Count > 0 || processRequestQueue.Count > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(enqueueRequestInterval), cancellationToken: cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                if (showLogs)
                {
                    Debug.LogError($"[{gameObject.name}] Error in queue processing: {ex.Message}");
                }
            }
            finally
            {
                isProcessingQueue = false;
            }
        }

        /// <summary>
        /// Process a single GraphQL request
        /// </summary>
        protected virtual async UniTask ProcessGraphQLRequestAsync(
            GraphQLQueueItem queueItem,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] Processing GraphQL query from queue");
                }

                (bool success, string result) = await SendGraphQLQueryAsync(queueItem.Query, queueItem.Endpoints, callback: null, queueItem.CancellationToken);
                
                // Call the callback with the result
                queueItem.Callback?.Invoke(success, result);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled - don't log as error
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] GraphQL request cancelled");
                }
                queueItem.Callback?.Invoke(false, "{\"Error\":\"Request cancelled\"}");
            }
            catch (Exception ex)
            {
                if (showLogs)
                {
                    Debug.LogError($"[{gameObject.name}] Error processing GraphQL request: {ex.Message}");
                }
                queueItem.Callback?.Invoke(false, $"{{\"Error\":\"Processing failed: {ex.Message}\"}}");
            }
            finally
            {
                activeRequests--;
            }
        }

        /// <summary>
        /// Process a single process transactions request
        /// </summary>
        protected virtual async UniTask ProcessProcessTransactionsRequestAsync(
            ProcessTransactionsQueueItem queueItem,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] Processing process transactions query from queue");
                }

                (bool success, var result) = await GetProcessTransactionsAsync(
                    queueItem.ProcessId,
                    queueItem.AdditionalTags,
                    queueItem.First,
                    queueItem.GetData,
                    queueItem.Endpoints,
                    queueItem.FromTimestamp,
                    callback: null,
                    queueItem.CancellationToken);
                
                // Call the callback with the result
                queueItem.Callback?.Invoke(success, result);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled - don't log as error
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] Process transactions request cancelled");
                }
                queueItem.Callback?.Invoke(false, null);
            }
            catch (Exception ex)
            {
                if (showLogs)
                {
                    Debug.LogError($"[{gameObject.name}] Error processing process transactions request: {ex.Message}");
                }
                queueItem.Callback?.Invoke(false, null);
            }
            finally
            {
                activeRequests--;
            }
        }

        /// <summary>
        /// Get queue status information
        /// </summary>
        public virtual (int queuedGraphQL, int queuedProcessTransactions, int active, int maxConcurrent) GetQueueStatus()
        {
            return (requestQueue.Count, processRequestQueue.Count, activeRequests, maxConcurrentRequests);
        }

        /// <summary>
        /// Get current queue size (total across both queues)
        /// </summary>
        public int GetQueueSize()
        {
            return requestQueue.Count + processRequestQueue.Count;
        }

        /// <summary>
        /// Check if queue is currently being processed
        /// </summary>
        public bool IsProcessing()
        {
            return isProcessingQueue;
        }

        /// <summary>
        /// Clear all queues
        /// </summary>
        public virtual void ClearQueue()
        {
            // Clear GraphQL queue with callbacks
            while (requestQueue.Count > 0)
            {
                var request = requestQueue.Dequeue();
                if (callbackIfNotProcessed) request.Callback?.Invoke(false, "{\"Error\":\"Queue cleared\"}");
            }

            // Clear process transactions queue with callbacks  
            while (processRequestQueue.Count > 0)
            {
                var request = processRequestQueue.Dequeue();
                if (callbackIfNotProcessed) request.Callback?.Invoke(false, null);
            }
            
            if (showLogs)
            {
                Debug.Log($"[{gameObject.name}] All queues cleared");
            }
        }

        /// <summary>
        /// Override force stop to also clear queues
        /// </summary>
        public override void ForceStopAndReset()
        {
            base.ForceStopAndReset();
            ClearQueue();
            activeRequests = 0;
            isProcessingQueue = false;
        }
    }
}