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
    public class GraphQLHandler : MonoBehaviour
    {
        [Header("Debug")]
        public bool showLogs = true;

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
        public bool retryOnFailure = true;
        public List<int> retryDelays = new List<int> { 3, 10, 30 };
        public bool increaseRetryDelay = true;

        protected int timeout = 120;
        protected int currentEndpointIndex = 0;
        protected int retryIndex = 0;

        // Single shared cancellation token source for ALL operations
        private CancellationTokenSource _allOperationsCancellationTokenSource;

        protected virtual void Start()
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

        /// <summary>
        /// Get transaction data by ID from Arweave (async method with optional callback)
        /// </summary>
        /// <param name="transactionId">The transaction ID to fetch data for</param>
        /// <param name="callback">Optional callback with success status and data string</param>
        /// <returns>Transaction data string or null if failed</returns>
        public virtual async UniTask<string> GetTransactionDataAsync(string transactionId, Action<bool, string> callback = null)
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
                    return data;
                }
                else
                {
                    if (showLogs) 
                    {
                        Debug.LogWarning($"[{gameObject.name}] Failed to fetch transaction data for {transactionId}: {request.error}");
                    }
                    callback?.Invoke(false, null);
                    return null;
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
                return null;
            }
            catch (Exception ex)
            {
                if (showLogs) 
                {
                    Debug.LogError($"[{gameObject.name}] Exception fetching transaction data: {ex.Message}");
                }
                callback?.Invoke(false, null);
                return null;
            }
        }

        /// <summary>
        /// Send a GraphQL query (async method with optional callback)
        /// </summary>
        /// <param name="query">The GraphQL query string</param>
        /// <param name="callback">Optional callback with success status and JSON response</param>
        /// <param name="endpoints">Optional custom endpoints to use (uses default if null)</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the request</param>
        /// <returns>JSON response string or null if failed</returns>
        public virtual async UniTask<string> SendGraphQLQueryAsync(string query, Action<bool, string> callback = null, List<string> endpoints = null, CancellationToken cancellationToken = default)
        {
            endpoints = endpoints ?? graphqlEndpoints;
            
            if (endpoints == null || endpoints.Count == 0)
            {
                if (showLogs) Debug.LogError($"[{gameObject.name}] No GraphQL endpoints provided");
                callback?.Invoke(false, null);
                return null;
            }

            // Try each endpoint in order
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

                    // Use provided cancellation token, or fall back to shared token
                    var effectiveToken = cancellationToken == default ? GetSharedCancellationToken() : cancellationToken;
                    await request.SendWebRequest().ToUniTask(cancellationToken: effectiveToken);

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string responseData = request.downloadHandler.text;

                        if (showLogs)
                        {
                            Debug.Log($"[{gameObject.name}] GraphQL Success: {responseData}");
                        }

                        // Success
                        retryIndex = 0;
                        callback?.Invoke(true, responseData);
                        return responseData;
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
                    // Operation was cancelled - don't retry, exit immediately
                    if (showLogs)
                    {
                        Debug.Log($"[{gameObject.name}] GraphQL request cancelled");
                    }
                    callback?.Invoke(false, null);
                    return null;
                }
                catch (Exception ex)
                {
                    if (showLogs)
                    {
                        Debug.LogError($"[{gameObject.name}] GraphQL request exception: {ex.Message}");
                    }
                }

                // If this was the last endpoint and we have retry enabled, try with delays
                if (i == endpoints.Count - 1 && retryOnFailure && retryIndex < retryDelays.Count)
                {
                    if (showLogs)
                    {
                        Debug.Log($"[{gameObject.name}] All endpoints failed, retrying in {retryDelays[retryIndex]} seconds");
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(retryDelays[retryIndex]), cancellationToken: GetSharedCancellationToken());

                    if (increaseRetryDelay)
                    {
                        retryIndex++;
                    }

                    // Retry with all endpoints again
                    i = -1; // Will be incremented to 0 in next loop iteration
                    continue;
                }
            }

            // All endpoints and retries failed
            if (showLogs) Debug.LogError($"[{gameObject.name}] All GraphQL endpoints failed after retries");
            callback?.Invoke(false, null);
            return null;
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
        /// <returns>Dictionary where key is txID and value is Message, or null if failed</returns>
        public virtual async UniTask<Dictionary<string, Message>> GetProcessTransactionsAsync(
            string processId,
            List<Tag> additionalTags = null,
            Action<bool, Dictionary<string, Message>> callback = null,
            int first = 1,
            bool getData = true,
            List<string> endpoints = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build and send GraphQL query
                string query = BuildProcessTransactionsQuery(processId, additionalTags, first);
                string graphqlResponse = await SendGraphQLQueryAsync(query, null, endpoints, cancellationToken);

                if (string.IsNullOrEmpty(graphqlResponse))
                {
                    callback?.Invoke(false, null);
                    return null;
                }

                // Parse GraphQL response
                var jsonResponse = SimpleJSON.JSON.Parse(graphqlResponse);
                var edges = jsonResponse["data"]["transactions"]["edges"];
                
                if (edges == null || edges.Count == 0)
                {
                    var emptyResult = new Dictionary<string, Message>();
                    callback?.Invoke(true, emptyResult);
                    return emptyResult;
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
                    return result;
                }

                // Fetch transaction data for all transactions in parallel
                var dataFetchTasks = new List<UniTask>();
                var dataResults = new Dictionary<string, string>();
                
                foreach (var txId in result.Keys.ToList())
                {
                    var task = UniTask.Create(async () =>
                    {
                        string data = await GetTransactionDataAsync(txId);
                        if (!string.IsNullOrEmpty(data))
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
                return result;
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - exit immediately
                if (showLogs)
                {
                    Debug.Log($"[{gameObject.name}] GetProcessTransactionsAsync cancelled");
                }
                callback?.Invoke(false, null);
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in GetProcessTransactionsAsync: {ex.Message}");
                callback?.Invoke(false, null);
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// Build a GraphQL query for transactions from a specific process
        /// </summary>
        protected string BuildProcessTransactionsQuery(string processId, List<Tag> additionalTags, int first)
        {
            // Build the tags array in proper GraphQL syntax (not JSON)
            var tagsList = new List<string>();

            // Add From-Process tag
            tagsList.Add($"{{name: \"From-Process\", values: [\"{processId}\"]}}");

            // Add Data-Protocol tag for AO transactions (helps with faster indexing)
            tagsList.Add($"{{name: \"Data-Protocol\", values: [\"ao\"]}}");

            // Add additional tags if provided
            if (additionalTags != null)
            {
                foreach (var tag in additionalTags)
                {
                    tagsList.Add($"{{name: \"{tag.Name}\", values: [\"{tag.Value}\"]}}");
                }
            }

            string tagsString = $"[{string.Join(", ", tagsList)}]";

            // Build the complete GraphQL query string
            string graphqlQuery = $@"{{
            transactions(
                first: {first}
                tags: {tagsString}
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

            // Cancel ALL running operations
            _allOperationsCancellationTokenSource?.Cancel();
            _allOperationsCancellationTokenSource?.Dispose();

            // Create fresh cancellation token source
            _allOperationsCancellationTokenSource = new CancellationTokenSource();

            // Reset state
            retryIndex = 0;
            currentEndpointIndex = 0;
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