using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using SimpleJSON;
using System.Threading;

namespace Permaverse.AO
{
	/// <summary>
	/// Paginated HyperBeam path handler that inherits from HyperBeamPathHandler
	/// Provides pagination functionality for path requests with scroll rect support
	/// 
	/// This handler replaces the HyperBeam path functionality that was previously 
	/// embedded in PaginatedMessageHandler. Features include:
	/// - Scroll rect integration with automatic next page loading
	/// - Configurable threshold for triggering next page loads
	/// - Loading state management with optional loading icon
	/// - Pagination state tracking (currentPageIndex, hasNextPage)
	/// - Support for both enqueued and direct requests
	/// - Automatic pagination data parsing from JSON responses
	/// </summary>
	public class PaginatedHyperBeamPathHandler : EnqueueHyperBeamPathHandler
	{
		[Header("Pagination Settings")]
		public ScrollRect scrollRect;
		public int pageSize = 30;
		public float threshold = 0.1f;  // How close to the bottom must the user scroll before loading the next page
		public GameObject loadingIcon;

        protected bool isLoading = false;  // To prevent multiple loads at the same time
		protected bool hasNextPage = false;  // This should be updated based on the last fetched data

		// Current pagination state using PathRequest struct
		protected int currentPageIndex = 0;

		/// <summary>
		/// Force stop all running operations and reset state
		/// Override to also reset pagination state
		/// </summary>
		public override void ForceStopAndReset()
		{
			base.ForceStopAndReset();
			
			// Reset pagination state
			ResetPagination();
			
			// Reset loading state
			isLoading = false;
			hasNextPage = false;
			
			if (loadingIcon != null)
				loadingIcon.SetActive(false);
			
			if (showLogs)
				Debug.Log("[PaginatedHyperBeamPathHandler] Pagination state reset");
		}

		protected override void Start()
		{
			base.Start();
			if (scrollRect != null)
				scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
		}

		protected override void OnDestroy()
		{
			if (scrollRect != null)
				scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
			base.OnDestroy();
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
			if (showLogs) Debug.Log($"[{gameObject.name}] Loading next page...");
			
			// Clear queue to avoid conflicts with old requests
			ClearQueue();
			
			var request = currentRequest.Value;
			SendPaginatedRequestAsync(request.pid, request.methodName, currentRequest.Value.tags, request.callback, currentPageIndex + 1, enqueue: false, request.moduleId).Forget();
		}

		/// <summary>
		/// Send paginated HyperBeam request with automatic pagination support
		/// </summary>
		public async UniTask SendPaginatedRequestAsync(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, int pageIndex, bool enqueue = false, string moduleId = null, CancellationToken cancellationToken = default)
		{
			// Store current pagination state using PathRequest struct
			currentRequest = new PathRequest
			{
				pid = pid,
				methodName = methodName,
				tags = tags,
				callback = callback,
				serialize = true,
				moduleId = moduleId,
				cancellationToken = cancellationToken == default ? GetSharedCancellationToken() : cancellationToken
			};
			currentPageIndex = pageIndex;

			List<Tag> completeTags = new List<Tag>(tags)
			{
				new Tag("PageIndex", pageIndex.ToString()),
				new Tag("PageSize", pageSize.ToString())
			};

			// Create callback that processes pagination and calls original callback
			Action<bool, string> paginationCallback = (result, response) =>
			{
				ProcessPaginationData(result, response);
				callback?.Invoke(result, response);
			};

			if (enqueue)
			{
				EnqueueHyperBeamRequest(pid, methodName, completeTags, paginationCallback, true, moduleId);
			}
			else
			{
				var (success, result) = await SendDynamicRequestAsync(pid, methodName, completeTags, true, moduleId, true, callback:null, customIgnoreHttpErrors:null);
				paginationCallback(success, result);
			}
		}

		/// <summary>
		/// Process pagination response data and update state
		/// </summary>
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
						currentPageIndex = dataNode["CurrentPage"].AsInt;
					}

					if (dataNode.HasKey("HasNextPage"))
					{
						hasNextPage = dataNode["HasNextPage"].AsBool;
					}

					if (showLogs)
					{
						Debug.Log($"[{gameObject.name}] Pagination processed: Page {currentPageIndex}, HasNext: {hasNextPage}");
					}
				}
				catch (Exception e)
				{
					if (showLogs) Debug.LogError($"[{gameObject.name}] Failed to parse pagination response: {e.Message}");
				}
			}

			isLoading = false;
			if (loadingIcon != null) loadingIcon.SetActive(false);
		}

		/// <summary>
		/// Reset pagination state and UI
		/// </summary>
		public void ResetPagination()
		{
			isLoading = false;
			hasNextPage = false;
			currentPageIndex = 0;
			
			// Hide loading icon if active
			if (loadingIcon != null) 
				loadingIcon.SetActive(false);

			// Clear pagination request
			currentRequest = null;
		}
	}
}
