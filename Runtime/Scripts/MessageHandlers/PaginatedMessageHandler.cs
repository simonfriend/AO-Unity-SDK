using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;
using SimpleJSON;

namespace Permaverse.AO
{
	public class PaginatedMessageHandler : EnqueueMessageHandler
	{
		[Header("PaginatedMessageHandler")]
		public ScrollRect scrollRect;
		public float threshold = 0.1f;  // How close to the bottom must the user scroll before loading the next page
		public bool isLoading = false;  // To prevent multiple loads at the same time
		public bool hasNextPage = false;  // This should be updated based on the last fetched data
		public int pageSize = 30;

		public GameObject loadingIcon;

		[Header("HyperBEAM Support")]
		public string hyperBeamMethodName = "";  // Method name for HyperBEAM (e.g., "GetLeaderboard")
		public bool useHyperBeamPath = false;   // If true, use HyperBEAM path requests (string callback)
		protected string pid;
		protected List<Tag> tags;
		protected Action<bool, NodeCU> callback;
		protected Action<bool, string> hyperBeamCallback;
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
			if (!string.IsNullOrEmpty(pid) && !isLoading && hasNextPage && position.y <= threshold)
			{
				LoadNextPage();
			}
		}

		private void LoadNextPage()
		{
			isLoading = true;
			if (loadingIcon != null) loadingIcon.SetActive(true);
			Debug.Log("Loading next page...");
			if (useHyperBeamPath)
			{
				SendPaginatedHyperBeamPathRequest(pid, hyperBeamMethodName, tags, hyperBeamCallback, currentPageIndex + 1);
			}
			else
			{
				SendPaginatedRequest(pid, tags, callback, currentPageIndex + 1);	
			}
		}

		public void SendPaginatedRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, int pageIndex, bool enqueue = false, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false)
		{
			this.pid = pid;
			this.callback = callback;
			this.tags = tags;

			List<Tag> completeTags = new List<Tag>(this.tags)
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
				EnqueueRequest(pid, completeTags, unifiedCallback, method: method, useMainWallet: useMainWallet);
			}
			else
			{
				SendRequest(pid, completeTags, unifiedCallback, method: method, useMainWallet: useMainWallet);
			}
		}

		// Convenience method for HyperBEAM path requests (string callback)
		public void SendPaginatedHyperBeamPathRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, int pageIndex, bool enqueue = false, string moduleId = null)
		{
			this.pid = pid;
			this.tags = tags;
			hyperBeamMethodName = methodName;
			useHyperBeamPath = true;
			hyperBeamCallback = callback;
			
			List<Tag> completeTags = new List<Tag>(this.tags)
			{
				new Tag("PageIndex", pageIndex.ToString()),
				new Tag("PageSize", pageSize.ToString())
			};

			// Create callback that processes pagination and calls original callback
			Action<bool, string> hyperBeamStringCallback = (result, response) =>
			{
				ProcessPaginationData(result, response);
				callback?.Invoke(result, response);
			};

			if(enqueue)
			{
				EnqueueHyperBeamRequest(pid, methodName, completeTags, hyperBeamStringCallback);
			}
			else
			{
				SendHyperBeamRequest(pid, methodName, completeTags, hyperBeamStringCallback, moduleId: moduleId);
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

			// Clear pagination references
			pid = null;
			tags = null;
			callback = null;
			hyperBeamCallback = null;

			// Call base class implementation (EnqueueMessageHandler)
			base.ForceStopAndReset();
		}
	}
}