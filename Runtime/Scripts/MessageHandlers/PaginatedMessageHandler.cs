using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;
using SimpleJSON;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Permaverse.AO
{
	public class PaginatedMessageHandler : EnqueueMessageHandler
	{
		[Header("Pagination Settings")]
		public ScrollRect scrollRect;
		public int pageSize = 30;
		public float threshold = 0.1f;  // How close to the bottom must the user scroll before loading the next page
		public GameObject loadingIcon;

		protected bool isLoading = false;  // To prevent multiple loads at the same time
		protected bool hasNextPage = false;  // This should be updated based on the last fetched data

		// Pagination-specific state tracking
		protected int currentPageIndex = 0;

		void Start()
		{
			if (scrollRect != null)
				scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
		}

		void OnDestroy()
		{
			if (scrollRect != null)
				scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
		}

		private void OnScrollValueChanged(Vector2 position)
		{
			if (currentRequest.HasValue && !string.IsNullOrEmpty(currentRequest.Value.pid) && !isLoading && hasNextPage && position.y <= threshold)
			{
				LoadNextPage();
			}
		}

		private void LoadNextPage()
		{
			if (!currentRequest.HasValue) return;

			isLoading = true;
			if (loadingIcon != null) loadingIcon.SetActive(true);
			Debug.Log("Loading next page...");

			// Clear queue to avoid conflicts with old requests
			ClearQueue();

			var request = currentRequest.Value;
			SendPaginatedRequestAsync(request.pid, request.tags, request.callback, currentPageIndex + 1, false, request.method, request.useMainWallet).Forget();
		}

		public async UniTask SendPaginatedRequestAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, int pageIndex, bool enqueue = false, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, CancellationToken cancellationToken = default)
		{
			// Store current request in struct from parent class
			currentRequest = new MessageRequest
			{
				pid = pid,
				tags = tags,
				callback = callback,
				data = null,
				method = method,
				useMainWallet = useMainWallet,
				walletType = WalletType.Default,
				cancellationToken = cancellationToken == default ? GetSharedCancellationToken() : cancellationToken
			};
			currentPageIndex = pageIndex;

			List<Tag> completeTags = new List<Tag>(tags)
			{
				new Tag("PageIndex", pageIndex.ToString()),
				new Tag("PageSize", pageSize.ToString())
			};

			// Create unified callback that handles pagination logic
			Action<bool, NodeCU> unifiedCallback = (result, response) =>
			{
				string responseData = null;
				if (result && response != null)
				{
					responseData = response.Messages[0].Data;
				}
				ProcessPaginationData(result, responseData);
				callback?.Invoke(result, response);
			};

			if (enqueue)
			{
				// Use enqueue functionality from base class
				EnqueueRequest(pid, completeTags, unifiedCallback, null, method, useMainWallet);
			}
			else
			{
				// Use await for cleaner async flow
				var (success, result) = await SendRequestAsync(pid, completeTags, null, method, useMainWallet);
				unifiedCallback(success, result);
			}
		}

		private void ProcessPaginationData(bool result, string responseData)
		{
			if (result && !string.IsNullOrEmpty(responseData))
			{
				try
				{
					JSONNode jsonNode = JSON.Parse(responseData);

					// For HyperBEAM responses, check both direct fields and Data field
					JSONNode dataNode = null;

					// Check if this is a wrapped response (like GetLeaderboard)
					if (jsonNode.HasKey("Data"))
					{
						dataNode = JSON.Parse(jsonNode["Data"]);
					}
					else
					{
						dataNode = jsonNode;
					}

					if (dataNode.HasKey("CurrentPage"))
					{
						// Update currentPageIndex directly
						currentPageIndex = dataNode["CurrentPage"].AsInt;
					}

					if (dataNode.HasKey("HasNextPage"))
					{
						hasNextPage = dataNode["HasNextPage"].AsBool;
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"Failed to parse pagination response: {e.Message}");
				}
			}

			isLoading = false;
			if (loadingIcon != null) loadingIcon.SetActive(false);
		}

		public override void ForceStopAndReset()
		{
			// Reset pagination state
			isLoading = false;
			hasNextPage = false;
			currentPageIndex = 0;

			// Hide loading icon if active
			if (loadingIcon != null)
				loadingIcon.SetActive(false);

			// Clear pagination request
			currentRequest = null;

			// Call base class implementation (EnqueueMessageHandler)
			base.ForceStopAndReset();
		}
	}
}