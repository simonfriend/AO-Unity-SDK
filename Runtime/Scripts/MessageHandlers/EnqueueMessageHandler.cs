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

		// UniTask versions for zero-allocation performance
		public virtual void EnqueueRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, bool useMainWallet = false, WalletType walletType = WalletType.Default)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && asyncRequestQueue.Count > 0)
			{
				return;
			}

			// Use the new async method with proper await instead of callback
			var task = EnqueueRequestInternalAsync(pid, tags, callback, data, method, useMainWallet, walletType, GetSharedCancellationToken());
			asyncRequestQueue.Enqueue(task);
			
			if (!isProcessing)
			{
				ProcessQueueAsync(GetSharedCancellationToken()).Forget();
			}
		}

		private async UniTask EnqueueRequestInternalAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data, NetworkMethod method, bool useMainWallet, WalletType walletType, CancellationToken cancellationToken)
		{
			// Use the new centralized retry logic with await
			var (success, result) = await SendRequestAsync(pid, tags, null, data, method, useMainWallet, walletType, cancellationToken);
			
			// Call the callback with the final result
			callback?.Invoke(success, result);
		}

		public virtual void EnqueueHyperBeamRequest(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, bool serialize = true, string moduleId = null)
		{
			float timeSinceLastRequest = Time.time - lastRequestTime;

			if (timeSinceLastRequest < enqueueRequestInterval && limitToOneRequest && asyncRequestQueue.Count > 0)
			{
				return;
			}

			// Use await pattern for cleaner async code
			var task = EnqueueHyperBeamRequestInternalAsync(pid, methodName, tags, callback, serialize, moduleId, GetSharedCancellationToken());
			asyncRequestQueue.Enqueue(task);
			
			if (!isProcessing)
			{
				ProcessQueueAsync(GetSharedCancellationToken()).Forget();
			}
		}

		private async UniTask EnqueueHyperBeamRequestInternalAsync(string pid, string methodName, List<Tag> tags, Action<bool, string> callback, bool serialize, string moduleId, CancellationToken cancellationToken)
		{
			// Use the existing HyperBeam path logic with await
			await SendHyperBeamRequestAsync(pid, methodName, tags, callback, serialize, moduleId, cancellationToken);
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