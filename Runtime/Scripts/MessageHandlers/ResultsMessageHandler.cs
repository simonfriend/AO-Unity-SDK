using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace Permaverse.AO
{
	public class ResultsMessageHandler : MonoBehaviour
	{
		[Header("ResultsMessageHandler")]
		public string pid;
		public int initialResultSize = 10;
		public float askInterval = 5;
		public int resultsToKeep = 10;
		public long initialFrom = -1;
		public string baseUrl = "https://cu.ao-testnet.xyz/results/";
		// public bool callbackInAscOrder = true;

		public List<Results> lastResults;

		public Action<Results> ResultsCallback;

		[Header("Debug")]
		public bool showLogs = true;

		[SerializeField] protected string firstCursor = null;

		protected int timeout = 60;
		// protected Coroutine getResultsCoroutine;
		
		// Single shared cancellation token source for all operations
		private CancellationTokenSource _allOperationsCancellationTokenSource;

		private void Awake()
		{
			// Initialize the cancellation token source
			_allOperationsCancellationTokenSource = new CancellationTokenSource();
		}

		private void OnDestroy()
		{
			// Automatically cancel all operations when GameObject is destroyed
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
		}

		/// <summary>
		/// Get shared cancellation token for all operations
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
	
		public void StartGettingResults(string pid = null, int initialResultSize = -1, float askInterval = -1, long initialFrom = -1)
		{
			StopGettingResults();
			lastResults.Clear();

			firstCursor = null;

			if (pid != null)
			{
				this.pid = pid;
			}

			if (initialResultSize != -1)
			{
				this.initialResultSize = initialResultSize;
			}

			if (askInterval != -1)
			{
				this.askInterval = askInterval;
			}

			if (initialFrom != -1)
			{
				this.initialFrom = initialFrom;
			}

			// Start async operation
			GetResultsAsync(GetSharedCancellationToken()).Forget();
		}

		public void StopGettingResults()
		{
			// Cancel operations using the token
			_allOperationsCancellationTokenSource?.Cancel();
			_allOperationsCancellationTokenSource?.Dispose();
			_allOperationsCancellationTokenSource = new CancellationTokenSource();
			
			// Keep old coroutine cleanup for compatibility
			// if (getResultsCoroutine != null)
			// {
			// 	StopCoroutine(getResultsCoroutine);
			// }
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

		/* COMMENTED OUT - OLD COROUTINE VERSION
		protected IEnumerator GetResultsCoroutine()
		{
			lastResults = new List<Results>();

			while (true)
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

				bool done = false;
				Results result = null;
				string jsonResult = null;

				Action<Results, string> callback = (Results res, string jsonRes) =>
				{
					done = true;
					result = res;
					jsonResult = jsonRes;
				};

				float timeElapsed = 0f;
				if (showLogs)
				{
					Debug.Log($"[{gameObject.name}] Sending request with url: {url}");
				}

				StartCoroutine(SendHttpPostRequest(url, callback));

				while (!done)
				{
					timeElapsed += Time.deltaTime;
					yield return null;
				}

				if (result != null && result.Edges.Count != 0)
				{
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

				yield return new WaitForSeconds(askInterval);
			}
		}
		*/

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

		/* COMMENTED OUT - OLD COROUTINE VERSION
		protected IEnumerator SendHttpPostRequest(string url, Action<Results, string> callback)
		{
			UnityWebRequest request = new UnityWebRequest(url, "GET");
			request.downloadHandler = new DownloadHandlerBuffer();
			request.timeout = timeout;
			request.SetRequestHeader("Content-Type", "application/json");

			yield return request.SendWebRequest();

			Results results;

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"[{gameObject.name}] HTTP Post Error: {request.error}");

				results = null;

				callback(results, request.error);
			}
			else
			{
				results = new Results(request.downloadHandler.text);

				callback(results, request.downloadHandler.text);
			}
		}
		*/
	}
}