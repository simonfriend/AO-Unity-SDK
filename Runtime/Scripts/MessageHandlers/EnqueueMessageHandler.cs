using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Permaverse.AO
{
	public class EnqueueMessageHandler : MessageHandler
	{
		[Header("EnqueueMessageHandler")]
		public float enqueueRequestInterval = 5f;
		public bool limitToOneRequest = true;

		protected Queue<IEnumerator> requestQueue = new Queue<IEnumerator>();
		protected bool isProcessing = false;
		protected float lastRequestTime = 0f;

		public override void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false)
		{
			lastRequestTime = Time.time;
			base.SendRequest(pid, tags, callback, data, method, useMainWallet);
		}

		public virtual void EnqueueRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && requestQueue.Count > 0)
			{
				return;
			}

			requestQueue.Enqueue(SendRequestCoroutine(pid, tags, callback, data, method, useMainWallet));
			if (!isProcessing)
			{
				StartCoroutine(ProcessQueue());
			}
		}

		protected IEnumerator ProcessQueue()
		{
			isProcessing = true;

			while (requestQueue.Count > 0)
			{
				lastRequestTime = Time.time;
				yield return StartCoroutine(requestQueue.Dequeue());

				while ((Time.time - lastRequestTime) < enqueueRequestInterval)
				{
					yield return null;
				}
			}

			isProcessing = false;
		}
	}
}