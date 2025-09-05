using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using SimpleJSON;

namespace Permaverse.AO
{
    /// <summary>
    /// GraphQL handler for Arweave network communication with endpoint fallback and retry logic
    /// Provides async methods for GraphQL queries and transaction data fetching
    /// </summary>
    public class GraphQLHandler : MonoBehaviour
    {
        [Header("GraphQL Settings")]
        [Tooltip("GraphQL endpoints to try in order (fallback system)")]
        public List<string> graphqlEndpoints = new List<string>
        {
            "https://arweave.net/graphql",
            "https://arweave-search.goldsky.com/graphql",
            "https://g8way.io/graphql",
            "https://arweave.dev/graphql"
        };

        [Header("Retry Settings")]
        [Tooltip("Whether to retry failed requests")]
        public bool resendIfResultFalse = true;
        
        [Tooltip("Maximum number of retry attempts for failed requests")]
        public int maxRetries = 5;
        
        [Tooltip("Delay between retry attempts")]
        public List<int> resendDelays = new List<int> { 1, 5, 10, 30 };
        
        protected float defaultDelay = 5f;

        protected int timeout = 120;

        // Single shared cancellation token source for ALL operations (simple StopAllCoroutines equivalent)
        private CancellationTokenSource _allOperationsCancellationTokenSource;

        protected bool showLogs => AOConnectManager.main.showLogs;

        protected virtual void Start()
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

        /// <summary>
        /// Get transaction data by ID from Arweave (async method with optional callback)
        /// </summary>
        /// <param name="transactionId">The transaction ID to fetch data for</param>
        /// <param name="callback">Optional callback with success status and data string</param>
        /// <returns>Tuple with success status and transaction data string</returns>
        public virtual async UniTask<(bool success, string result)> GetTransactionDataAsync(string transactionId, Action<bool, string> callback = null)
        {
            try
            {
                string arweaveUrl = $"https://arweave.net/{transactionId}";
                
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] Fetching transaction data: {arweaveUrl}");
                }

                using UnityWebRequest request = UnityWebRequest.Get(arweaveUrl);
                request.timeout = timeout;

                await request.SendWebRequest().ToUniTask(cancellationToken: GetSharedCancellationToken());

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string data = request.downloadHandler.text;
                    
                    if (showLogs)
                    {
                        Debug.Log($"[{gameObject.name}] Transaction data fetched successfully: {data.Length} characters");
                    }

                    callback?.Invoke(true, data);
                    return (true, data);
                }
                else
                {
                    if (showLogs) 
                    {
                        Debug.LogWarning($"[{gameObject.name}] Failed to fetch transaction data for {transactionId}: {request.error}");
                    }
                    callback?.Invoke(false, null);
                    return (false, null);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - exit immediately
                if (showLogs) 
                {
                    Debug.Log($"[{gameObject.name}] Transaction data fetch cancelled");
                }
                callback?.Invoke(false, null);
                return (false, null);
            }
            catch (Exception ex)
            {
                if (showLogs) 
                {
                    Debug.LogError($"[{gameObject.name}] Exception fetching transaction data: {ex.Message}");
                }
                callback?.Invoke(false, null);
                return (false, null);
            }
        }

        /// <summary>
        /// Send a GraphQL query with centralized retry logic (async method with optional callback)
        /// </summary>
        /// <param name="query">The GraphQL query string</param>
        /// <param name="callback">Optional callback with success status and JSON response</param>
        /// <param name="endpoints">Optional custom endpoints to use (uses default if null)</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the request</param>
        /// <returns>Tuple with success status and JSON response string</returns>
        public virtual async UniTask<(bool success, string result)> SendGraphQLQueryAsync(string query, List<string> endpoints = null, Action<bool, string> callback = null, CancellationToken cancellationToken = default)
        {
            // Use shared cancellation token if none provided
            if (cancellationToken == default)
            {
                cancellationToken = GetSharedCancellationToken();
            }

            endpoints = endpoints ?? graphqlEndpoints;
            
            if (endpoints == null || endpoints.Count == 0)
            {
                if (showLogs) Debug.LogError($"[{gameObject.name}] No GraphQL endpoints provided");
                callback?.Invoke(false, null);
                return (false, null);
            }

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Try the request with endpoint fallback
                    (bool success, string result) = await SendGraphQLQueryOnceAsync(query, endpoints, cancellationToken);
                    
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
                    float delay = attempt < resendDelays.Count ? resendDelays[attempt] : (resendDelays.Count > 0 ? resendDelays[resendDelays.Count - 1] : defaultDelay);
                    if (showLogs) Debug.Log($"[{gameObject.name}] Retrying GraphQL request in {delay} seconds (attempt {attempt + 2})");
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Request was cancelled during retry delay
                    if (showLogs) Debug.Log($"[{gameObject.name}] GraphQL request cancelled");
                    callback?.Invoke(false, null);
                    return (false, null);
                }
                catch (Exception ex)
                {
                    if (showLogs) Debug.LogError($"[{gameObject.name}] GraphQL request failed: {ex.Message}");
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
        /// Send a single GraphQL query attempt with endpoint fallback (no retry logic)
        /// </summary>
        private async UniTask<(bool success, string result)> SendGraphQLQueryOnceAsync(string query, List<string> endpoints, CancellationToken cancellationToken = default)
        {
            // Try each endpoint in order until one succeeds
            for (int i = 0; i < endpoints.Count; i++)
            {
                string endpoint = endpoints[i];

                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] Trying GraphQL endpoint [{i + 1}/{endpoints.Count}]: {endpoint}");
                }

                try
                {
                    // Create JSON body for GraphQL request
                    var requestBody = new JSONObject();
                    requestBody["query"] = query;
                    string jsonBody = requestBody.ToString();

                    if (showLogs)
                    {
                        Debug.Log($"[{gameObject.name}] GraphQL Query: {query}");
                    }

                    using UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = timeout;
                    request.SetRequestHeader("Content-Type", "application/json");

                    await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseData = request.downloadHandler.text;

                        if (showLogs)
                        {
                            Debug.Log($"[{gameObject.name}] GraphQL Success: {responseData}");
                        }

                        return (true, responseData);
                    }
                    else
                    {
                        if (showLogs)
                        {
                            Debug.LogWarning($"[{gameObject.name}] GraphQL endpoint {endpoint} failed: {request.error}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Re-throw cancellation - let the outer catch handle it
                    if (showLogs) Debug.Log($"[{gameObject.name}] GraphQL request cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    if (showLogs)
                    {
                        Debug.LogError($"[{gameObject.name}] GraphQL request exception: {ex.Message}");
                    }
                }
            }

            // All endpoints failed
            if (showLogs) Debug.LogError($"[{gameObject.name}] All GraphQL endpoints failed");
            return (false, null);
        }

        /// <summary>
        /// Get process transactions with optional data fetching (async method with optional callback)
        /// </summary>
        /// <param name="processId">The process ID to filter by</param>
        /// <param name="additionalTags">Additional tags to filter by</param>
        /// <param name="callback">Optional callback with success status and Dictionary of txID->Message</param>
        /// <param name="first">Number of transactions to fetch (default: 1)</param>
        /// <param name="getData">Whether to also fetch transaction data (default: true)</param>
        /// <param name="endpoints">Optional custom endpoints to use</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the request</param>
        /// <param name="fromTimestamp">Optional timestamp filter - only search transactions ingested after this time (Unix timestamp)</param>
        /// <returns>Tuple with success status and Dictionary where key is txID and value is Message</returns>
        public virtual async UniTask<(bool success, Dictionary<string, Message> result)> GetProcessTransactionsAsync(
            string processId,
            List<Tag> additionalTags = null,
            int first = 1,
            bool getData = true,
            List<string> endpoints = null,
            long? fromTimestamp = null,
            Action<bool, Dictionary<string, Message>> callback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build and send GraphQL query
                string query = BuildProcessTransactionsQuery(processId, additionalTags, first, fromTimestamp);
                (bool graphqlSuccess, string graphqlResponse) = await SendGraphQLQueryAsync(query, endpoints, callback: null, cancellationToken);

                if (!graphqlSuccess || string.IsNullOrEmpty(graphqlResponse))
                {
                    callback?.Invoke(false, null);
                    return (false, null);
                }

                // Parse GraphQL response
                var jsonResponse = JSON.Parse(graphqlResponse);
                var edges = jsonResponse["data"]["transactions"]["edges"];
                
                if (edges == null || edges.Count == 0)
                {
                    var emptyResult = new Dictionary<string, Message>();
                    callback?.Invoke(true, emptyResult);
                    return (true, emptyResult);
                }

                var result = new Dictionary<string, Message>();

                // Process each transaction
                for (int i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var node = edge["node"];
                    var txId = node["id"]?.Value;
                    var recipient = node["recipient"]?.Value;
                    
                    if (string.IsNullOrEmpty(txId)) continue;

                    // Create Message from GraphQL data
                    var messageJson = new JSONObject();
                    
                    // Add transaction ID as special tag
                    var tagsArray = new JSONArray();
                    tagsArray.Add(new Tag("Transaction-Id", txId).ToJson());
                    
                    // Add existing tags from GraphQL
                    var graphqlTags = node["tags"];
                    if (graphqlTags != null)
                    {
                        for (int j = 0; j < graphqlTags.Count; j++)
                        {
                            var tag = graphqlTags[j];
                            var name = tag["name"]?.Value;
                            var value = tag["value"]?.Value;
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                            {
                                tagsArray.Add(new Tag(name, value).ToJson());
                            }
                        }
                    }
                    
                    messageJson["tags"] = tagsArray;
                    // Use actual recipient from transaction instead of assuming processId
                    messageJson["target"] = !string.IsNullOrEmpty(recipient) ? recipient : processId;

                    // Create the message
                    var message = new Message(messageJson);
                    result[txId] = message;
                }

                // If getData is false, return now
                if (!getData)
                {
                    callback?.Invoke(true, result);
                    return (true, result);
                }

                // Fetch transaction data for all transactions in parallel
                var dataFetchTasks = new List<UniTask>();
                var dataResults = new Dictionary<string, string>();
                
                foreach (var txId in result.Keys.ToList())
                {
                    var task = UniTask.Create(async () =>
                    {
                        (bool success, string data) = await GetTransactionDataAsync(txId);
                        if (success && !string.IsNullOrEmpty(data))
                        {
                            dataResults[txId] = data;
                        }
                    });
                    dataFetchTasks.Add(task);
                }

                await UniTask.WhenAll(dataFetchTasks);

                // Add data to messages
                foreach (var kvp in dataResults)
                {
                    if (result.ContainsKey(kvp.Key))
                    {
                        result[kvp.Key].Data = kvp.Value;
                    }
                }

                callback?.Invoke(true, result);
                return (true, result);
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - exit immediately
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] GetProcessTransactionsAsync cancelled");
                }
                callback?.Invoke(false, null);
                return (false, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in GetProcessTransactionsAsync: {ex.Message}");
                callback?.Invoke(false, null);
                return (false, null);
            }
        }

        #region Private Methods

        /// <summary>
        /// Build a GraphQL query for transactions from a specific process
        /// </summary>
        /// <param name="processId">The process ID to query</param>
        /// <param name="additionalTags">Additional tags to filter by</param>
        /// <param name="first">Number of transactions to fetch</param>
        /// <param name="fromTimestamp">Optional timestamp filter - only search transactions ingested after this time</param>
        protected string BuildProcessTransactionsQuery(string processId, List<Tag> additionalTags, int first, long? fromTimestamp = null)
        {
            // Build the tags array in proper GraphQL syntax (not JSON)
            var tagsList = new List<string>
            {
                // Add From-Process tag
                $"{{name: \"From-Process\", values: [\"{processId}\"]}}",
                // Add Data-Protocol tag for AO transactions (helps with faster indexing)
                $"{{name: \"Data-Protocol\", values: [\"ao\"]}}"
            };

            // Add additional tags if provided
            if (additionalTags != null)
            {
                foreach (var tag in additionalTags)
                {
                    tagsList.Add($"{{name: \"{tag.Name}\", values: [\"{tag.Value}\"]}}");
                }
            }

            string tagsString = $"[{string.Join(", ", tagsList)}]";

            // Build the ingested_at filter if fromTimestamp is provided
            string ingestedAtFilter = "";
            if (fromTimestamp.HasValue)
            {
                ingestedAtFilter = $"ingested_at: {{min: {fromTimestamp.Value}}}";
            }

            // Build the complete GraphQL query string
            string graphqlQuery = $@"{{
            transactions(
                first: {first}
                tags: {tagsString}{(string.IsNullOrEmpty(ingestedAtFilter) ? "" : $"\n                {ingestedAtFilter}")}
            ) {{
                edges {{
                    node {{
                        id
                        recipient
                        tags {{
                            name
                            value
                        }}
                    }}
                }}
            }}
            }}";

            return graphqlQuery;
        }

        /// <summary>
        /// Force stop all operations and reset state
        /// </summary>
        public virtual void ForceStopAndReset()
        {
            if (showLogs)
            {
                Debug.Log($"[{gameObject.name}] ForceStopAndReset called - stopping ALL GraphQL operations");
            }

            // Cancel ALL running operations (UniTask equivalent of StopAllCoroutines)
            _allOperationsCancellationTokenSource?.Cancel();
            _allOperationsCancellationTokenSource?.Dispose();

            // Create fresh cancellation token source for new operations
            _allOperationsCancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Get the shared cancellation token
        /// </summary>
        protected CancellationToken GetSharedCancellationToken()
        {
            // Ensure we have a valid token source
            if (_allOperationsCancellationTokenSource == null || _allOperationsCancellationTokenSource.IsCancellationRequested)
            {
                _allOperationsCancellationTokenSource = new CancellationTokenSource();
            }

            return _allOperationsCancellationTokenSource.Token;
        }

        #endregion
    }
}