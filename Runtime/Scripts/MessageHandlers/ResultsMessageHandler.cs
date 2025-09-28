using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Permaverse.AO
{
	/// <summary>
	/// Handles polling and processing of AO process results using UniTask for async operations.
	/// Automatically fetches new results at regular intervals and provides callbacks for result processing.
	/// </summary>
	public class ResultsMessageHandler : MonoBehaviour
	{
		[Header("Configuration")]
		[Tooltip("Process ID to fetch results from")]
		public string pid;
		
		[Tooltip("Number of results to fetch on initial request")]
		public int initialResultSize = 10;
		
		[Tooltip("Interval in seconds between result polling requests")]
		public float askInterval = 5;
		
		[Tooltip("Maximum number of result batches to keep in memory")]
		public int resultsToKeep = 10;
		
		[Tooltip("Starting cursor position for initial fetch (-1 for latest)")]
		public long initialFrom = -1;
		
		[Tooltip("Base URL for the AO results endpoint")]
		public string baseUrl = "https://cu.ao-testnet.xyz/results/";
		[Header("Runtime Data")]
		[Tooltip("List of recent result batches received")]
		public List<Results> lastResults;

		[Header("Callbacks")]
		[Tooltip("Callback invoked when new results are received")]
		public Action<Results> ResultsCallback;

		protected bool showLogs => AOConnectManager.main.showLogs;


		protected string firstCursor = null;

		protected int timeout = 60;
		
		/// <summary>
		/// Shared cancellation token source for all async operations
		/// </summary>
		private CancellationTokenSource _allOperationsCancellationTokenSource;

		private void Awake()
		{
			_allOperationsCancellationTokenSource = new CancellationTokenSource();
		}

		private void OnDestroy()
		{
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
		}

		/// <summary>
		/// Gets or creates a shared cancellation token for all async operations
		/// </summary>
		private CancellationToken GetSharedCancellationToken()
		{
			if (_allOperationsCancellationTokenSource == null || _allOperationsCancellationTokenSource.Token.IsCancellationRequested)
			{
				_allOperationsCancellationTokenSource?.Dispose();
				_allOperationsCancellationTokenSource = new CancellationTokenSource();
			}
			return _allOperationsCancellationTokenSource.Token;
		}

		/// <summary>
		/// Starts polling for results from the specified AO process
		/// </summary>
		/// <param name="pid">Process ID to poll (null to use current)</param>
		/// <param name="initialResultSize">Number of results for initial fetch (-1 to use current)</param>
		/// <param name="askInterval">Polling interval in seconds (-1 to use current)</param>
		/// <param name="initialFrom">Starting cursor position (-1 to use current)</param>
		public void StartGettingResults(string pid = null, int initialResultSize = -1, float askInterval = -1, long initialFrom = -1)
		{
			StopGettingResults();
			lastResults.Clear();
			firstCursor = null;

			// Update configuration if new values provided
			if (pid != null) this.pid = pid;
			if (initialResultSize != -1) this.initialResultSize = initialResultSize;
			if (askInterval != -1) this.askInterval = askInterval;
			if (initialFrom != -1) this.initialFrom = initialFrom;

			// Start async polling operation
			GetResultsAsync(GetSharedCancellationToken()).Forget();
		}

		/// <summary>
		/// Stops polling for results and cancels all ongoing operations
		/// </summary>
		public void StopGettingResults()
		{
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
			_allOperationsCancellationTokenSource = new CancellationTokenSource();
		}

		/// <summary>
		/// Async version of GetResults using UniTask
		/// </summary>
		protected async UniTask GetResultsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				lastResults = new List<Results>();

				while (!cancellationToken.IsCancellationRequested)
				{
					string parameters;

					if (lastResults.Count == 0)
					{
						parameters = $"limit={initialResultSize}";

						if (initialFrom != -1)
						{
							parameters += $"&from={initialFrom}";
						}
					}
					else
					{
						parameters = $"from={lastResults[lastResults.Count - 1].Edges[0].Cursor}";
					}

					string url = baseUrl + pid + "?sort=DESC&" + parameters;

					if (showLogs)
					{
						Debug.Log($"[{gameObject.name}] Sending request with url: {url}");
					}

					float startTime = Time.time;
					var (result, jsonResult) = await SendHttpPostRequestAsync(url, cancellationToken);

					if (result != null && result.Edges.Count != 0)
					{
						float timeElapsed = Time.time - startTime;
						if (showLogs)
						{
							Debug.Log($"[{gameObject.name}] Sent request with url: {url} || Received Result after {timeElapsed}s: {jsonResult}");
						}

						if (lastResults.Count == 0)
						{
							firstCursor = result.Edges[result.Edges.Count - 1].Cursor;
						}

						lastResults.Add(result);

						if (lastResults.Count > resultsToKeep)
						{
							lastResults.RemoveAt(0);
						}

						if (ResultsCallback != null)
						{
							ResultsCallback.Invoke(result);
						}
					}

					await UniTask.Delay(TimeSpan.FromSeconds(askInterval), cancellationToken: cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested - no need to log
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{gameObject.name}] Error in GetResults: {ex.Message}");
			}
		}

		/// <summary>
		/// Async version of HTTP request using UniTask
		/// </summary>
		protected async UniTask<(Results, string)> SendHttpPostRequestAsync(string url, CancellationToken cancellationToken = default)
		{
			try
			{
				using (UnityWebRequest request = new UnityWebRequest(url, "GET"))
				{
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
						Debug.LogError($"[{gameObject.name}] HTTP Get Error: {ex.UnityWebRequest.error}");
					}

					if (request.result != UnityWebRequest.Result.Success)
					{
						Debug.LogError($"[{gameObject.name}] HTTP Post Error: {request.error}");
						return (null, request.error);
					}
					else
					{
						Results results = new Results(request.downloadHandler.text);
						return (results, request.downloadHandler.text);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested
				throw;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[{gameObject.name}] HTTP request failed: {ex.Message}");
				return (null, ex.Message);
			}
		}
	}
}