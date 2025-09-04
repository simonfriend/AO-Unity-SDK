using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Permaverse.AO
{
	public class EnqueueMessageHandler : MessageHandler
	{
		[Header("EnqueueMessageHandler")]
		public float enqueueRequestInterval = 5f;
		public bool limitToOneRequest = true;

		protected Queue<UniTask> asyncRequestQueue = new Queue<UniTask>();
		protected bool isProcessing = false;
		protected float lastRequestTime = 0f;

		public override void SendRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			lastRequestTime = Time.time;
			base.SendRequest(pid, tags, callback, data, method, useMainWallet, walletType);
		}

		// HyperBEAM path request method - returns string directly
		public virtual void SendHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, bool serialize = true, string moduleId = null)
		{
			lastRequestTime = Time.time;
			SendHyperBeamDynamicRequest(pid, methodName, tags, callback, serialize: serialize, moduleId: moduleId);
		}

		// UniTask versions for zero-allocation performance
		public virtual void EnqueueRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && asyncRequestQueue.Count > 0)
			{
				return;
			}

			var task = SendRequestAsync(pid, tags, callback, data, method, useMainWallet, walletType, GetSharedCancellationToken());
			asyncRequestQueue.Enqueue(task);
			
			if (!isProcessing)
			{
				ProcessQueueAsync(GetSharedCancellationToken()).Forget();
			}
		}

		public virtual void EnqueueHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, bool serialize = true, string moduleId = null)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && asyncRequestQueue.Count > 0)
			{
				return;
			}

			var task = SendHyperBeamRequestAsync(pid, methodName, tags, callback, serialize, moduleId, GetSharedCancellationToken());
			asyncRequestQueue.Enqueue(task);
			
			if (!isProcessing)
			{
				ProcessQueueAsync(GetSharedCancellationToken()).Forget();
			}
		}

		public override void ForceStopAndReset()
		{
			// Call base class implementation first (handles shared cancellation token)
			base.ForceStopAndReset();

			// Clear the request queues
			asyncRequestQueue.Clear();

			// Reset processing state
			isProcessing = false;
			lastRequestTime = 0f;
		}

		// UniTask async helper methods
		protected virtual async UniTask SendHyperBeamRequestAsync(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, bool serialize = true, string moduleId = null, CancellationToken cancellationToken = default)
		{
			string path = BuildHyperBeamDynamicPath(pid, methodName, tags, now: true, moduleId: moduleId);
			await SendHyperBeamPathAsync(path, callback, serialize, cancellationToken);
		}

		protected virtual async UniTask ProcessQueueAsync(CancellationToken cancellationToken = default)
		{
			isProcessing = true;

			try
			{
				while (asyncRequestQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
				{
					lastRequestTime = Time.time;
					var task = asyncRequestQueue.Dequeue();
					await task;

					while ((Time.time - lastRequestTime) < enqueueRequestInterval && !cancellationToken.IsCancellationRequested)
					{
						await UniTask.Yield(cancellationToken: cancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancellation is requested
			}
			finally
			{
				isProcessing = false;
			}
		}
	}
}