using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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

		public List<Results> lastResults;

		public Action<Results> ResultsCallback;

		[Header("Debug")]
		public bool showLogs = true;

		[SerializeField] protected string firstCursor = null;

		protected int timeout = 60;
		protected Coroutine getResultsCoroutine;

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

			getResultsCoroutine = StartCoroutine(GetResultsCoroutine());
		}

		public void StopGettingResults()
		{
			if (getResultsCoroutine != null)
			{
				StopCoroutine(getResultsCoroutine);
			}
		}

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

				StartCoroutine(SendHttpPostRequest(url, callback));

				while (!done)
				{
					yield return null;
				}

				if (result != null && result.Edges.Count != 0)
				{
					if (showLogs)
					{
						Debug.Log($"[{gameObject.name}] Sent request with url: {url} || Received Result: {jsonResult}");
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
	}
}