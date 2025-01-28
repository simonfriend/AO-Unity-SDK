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

		protected string pid;
		protected List<Tag> tags;
		protected Action<bool, NodeCU> callback;
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
			Debug.Log("Loading next page...");
			SendPaginatedRequest(pid, tags, callback, currentPageIndex + 1);
		}

		public void SendPaginatedRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, int pageIndex, bool enqueue = false, NetworkMethod method = NetworkMethod.Web2)
		{
			this.pid = pid;
			this.callback = callback;
			this.tags = tags;

			List<Tag> completeTags = new List<Tag>(this.tags)
		{
			new Tag("PageIndex", pageIndex.ToString()),
			new Tag("PageSize", pageSize.ToString())
		};

			if (enqueue)
			{
				EnqueueRequest(pid, completeTags, OnCallback, method: method);
			}
			else
			{
				SendRequest(pid, completeTags, OnCallback, method: method);
			}
		}

		private void OnCallback(bool result, NodeCU response)
		{
			JSONNode jsonNode = JSON.Parse(response.Messages[0].Data);

			if (jsonNode.HasKey("CurrentPage"))
			{
				currentPageIndex = jsonNode["CurrentPage"].AsInt;
			}

			if (jsonNode.HasKey("HasNextPage"))
			{
				hasNextPage = jsonNode["HasNextPage"].AsBool;
			}

			isLoading = false;

			callback.Invoke(result, response);
		}
	}
}