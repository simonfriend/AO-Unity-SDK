using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine;
using SimpleJSON;
using Cysharp.Threading.Tasks;

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
				SendPaginatedHyperBeamPathRequestAsync(pid, hyperBeamMethodName, tags, hyperBeamCallback, currentPageIndex + 1).Forget();
			}
			else
			{
				SendPaginatedRequestAsync(pid, tags, callback, currentPageIndex + 1).Forget();
			}
		}

		public async UniTask SendPaginatedRequestAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, int pageIndex, bool enqueue = false, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false)
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
				// Use await for cleaner async flow
				var (success, result) = await SendRequestAsync(pid, completeTags, null, null, method, useMainWallet);
				unifiedCallback(success, result);
			}
			else
			{
				// Use await for cleaner async flow
				var (success, result) = await SendRequestAsync(pid, completeTags, null, null, method, useMainWallet);
				unifiedCallback(success, result);
			}
		}

		// Async version for HyperBEAM path requests
		public async UniTask SendPaginatedHyperBeamPathRequestAsync(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, int pageIndex, bool enqueue = false, string moduleId = null)
		{
			this.pid = pid;
			this.tags = tags;
			hyperBeamMethodName = methodName;
			useHyperBeamPath = true;
			hyperBeamCallback = callback;
			if (!string.IsNullOrEmpty(moduleId))
			{
				luaModuleId = moduleId;
			}
			
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
				// Use await pattern for cleaner async code
				string path = BuildHyperBeamDynamicPath(pid, methodName, completeTags, now: true, moduleId: moduleId);
				await SendHyperBeamPathAsync(path, hyperBeamStringCallback, true);
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

		/// <summary>
		/// Send paginated HyperBEAM path request and return result as tuple
		/// This provides a consistent API with other tuple-returning methods
		/// </summary>
		public async UniTask<(bool success, string result)> SendPaginatedHyperBeamPathRequestAsync(string pid, string methodName, List<Tag> tags, int pageIndex, bool enqueue = false, string moduleId = null)
		{
			this.pid = pid;
			this.tags = tags;
			hyperBeamMethodName = methodName;
			useHyperBeamPath = true;
			if (!string.IsNullOrEmpty(moduleId))
			{
				luaModuleId = moduleId;
			}
			
			List<Tag> completeTags = new List<Tag>(this.tags)
			{
				new Tag("PageIndex", pageIndex.ToString()),
				new Tag("PageSize", pageSize.ToString())
			};

			if(enqueue)
			{
				// For enqueued requests, we need to use the callback version and convert to tuple
				var tcs = new UniTaskCompletionSource<(bool, string)>();
				
				Action<bool, string> callback = (success, response) =>
				{
					ProcessPaginationData(success, response);
					tcs.TrySetResult((success, response));
				};
				
				EnqueueHyperBeamRequest(pid, methodName, completeTags, callback);
				return await tcs.Task;
			}
			else
			{
				// Use the new tuple-returning method for direct requests
				string path = BuildHyperBeamDynamicPath(pid, methodName, completeTags, now: true, moduleId: moduleId);
				var (success, result) = await SendHyperBeamPathAsync(path, null, true);
				
				// Process pagination data
				ProcessPaginationData(success, result);
				
				return (success, result);
			}
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