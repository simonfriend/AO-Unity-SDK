using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Permaverse.AO
{
	/// <summary>
	/// Periodic message handler that extends EnqueueMessageHandler with periodic request capabilities.
	/// Uses its own cancellation token for periodic operations to allow granular control,
	/// while underlying requests use the shared cancellation token from the base MessageHandler.
	/// </summary>
	public class PeriodicMessageHandler : EnqueueMessageHandler
	{
		[Header("PeriodicMessageHandler")]
		public float periodicInterval = 10f;
		protected IEnumerator periodicRequestCoroutine;
		
		// Separate cancellation token specifically for periodic request control
		private CancellationTokenSource _periodicCancellationTokenSource;

		private void Start()
		{
			// Initialize the periodic cancellation token source
			_periodicCancellationTokenSource = new CancellationTokenSource();
		}

		private void OnDestroy()
		{
			// Cancel and dispose periodic operations
			_periodicCancellationTokenSource?.Cancel();
			_periodicCancellationTokenSource?.Dispose();
		}

		public virtual void SetPeriodicRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun)
		{
			CancelPeriodicRequest();
			
			// Start async periodic request using periodic-specific cancellation token
			// Individual requests will still use the shared token from base class
			PeriodicRequestAsync(pid, tags, callback, data, method, _periodicCancellationTokenSource.Token).Forget();
		}

		public void CancelPeriodicRequest()
		{
			// Cancel and recreate the periodic cancellation token
			_periodicCancellationTokenSource?.Cancel();
			_periodicCancellationTokenSource?.Dispose();
			_periodicCancellationTokenSource = new CancellationTokenSource();
			
			// Keep old coroutine cleanup for compatibility
			// if (periodicRequestCoroutine != null)
			// {
			// 	StopCoroutine(periodicRequestCoroutine);
			// 	periodicRequestCoroutine = null;
			// }
		}

		/// <summary>
		/// Async version of periodic request using UniTask.
		/// The periodicCancellationToken controls the periodic loop,
		/// while individual requests use the shared cancellation token from base class.
		/// </summary>
		protected virtual async UniTask PeriodicRequestAsync(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun, CancellationToken periodicCancellationToken = default)
		{
			try
			{
				while (!periodicCancellationToken.IsCancellationRequested)
				{
					// Enqueue the request - this will use the shared cancellation token internally
					EnqueueRequest(pid, tags, callback, data, method);
					
					// Wait for the periodic interval (controlled by periodic token)
					await UniTask.Delay(TimeSpan.FromSeconds(periodicInterval), cancellationToken: periodicCancellationToken);
					
					// Additional wait to ensure proper spacing based on last request time (controlled by periodic token)
					while ((Time.time - lastRequestTime) < periodicInterval && !periodicCancellationToken.IsCancellationRequested)
					{
						await UniTask.Yield(periodicCancellationToken);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when periodic cancellation is requested - no need to log
			}
			catch (Exception ex)
			{
				Debug.LogError($"[PeriodicMessageHandler] Error in periodic request: {ex.Message}");
			}
		}

		/* COMMENTED OUT - OLD COROUTINE VERSION
		protected virtual IEnumerator PeriodicRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun)
		{
			while (true)
			{
				EnqueueRequest(pid, tags, callback, data, method);
				yield return new WaitForSeconds(periodicInterval);

				while ((Time.time - lastRequestTime) < periodicInterval)
				{
					yield return null;
				}
			}
		}
		*/

		public override void ForceStopAndReset()
		{
			// Cancel any periodic requests first (granular control)
			CancelPeriodicRequest();

			// Call base class implementation (EnqueueMessageHandler) to stop all other operations
			base.ForceStopAndReset();
		}
	}
}