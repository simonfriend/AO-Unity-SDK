using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

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

		public override void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			lastRequestTime = Time.time;
			base.SendRequest(pid, tags, callback, data, method, useMainWallet, walletType);
		}

		// HyperBEAM path request method - returns string directly
		public virtual void SendHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, string moduleId = null)
		{
			lastRequestTime = Time.time;
			SendHyperBeamDynamicRequest(pid, methodName, tags, callback, moduleId: moduleId);
		}

		public virtual void EnqueueRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && requestQueue.Count > 0)
			{
				return;
			}

			requestQueue.Enqueue(SendRequestCoroutine(pid, tags, callback, data, method, useMainWallet, walletType));
			if (!isProcessing)
			{
				StartCoroutine(ProcessQueue());
			}
		}

		public virtual void EnqueueHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, string moduleId = null)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && requestQueue.Count > 0)
			{
				return;
			}

			requestQueue.Enqueue(SendHyperBeamRequestCoroutine(pid, methodName, tags, callback, moduleId));
			if (!isProcessing)
			{
				StartCoroutine(ProcessQueue());
			}
		}

		protected IEnumerator SendHyperBeamRequestCoroutine(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, string moduleId = null)
		{
			SendHyperBeamRequest(pid, methodName, tags, callback, moduleId);
			yield return null;
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

		public override void ForceStopAndReset()
		{
			// Call base class implementation first
			base.ForceStopAndReset();

			// Clear the request queue
			requestQueue.Clear();

			// Reset processing state
			isProcessing = false;
			lastRequestTime = 0f;
		}
	}
}