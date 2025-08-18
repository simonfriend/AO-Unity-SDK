using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace Permaverse.AO
{
	public class PeriodicMessageHandler : EnqueueMessageHandler
	{
		[Header("PeriodicMessageHandler")]
		public float periodicInterval = 10f;
		protected IEnumerator periodicRequestCoroutine;

		public virtual void SetPeriodicRequest(string pid, List<Tag> tags, Action<bool, NodeCU> callback, string data = null, NetworkMethod method = NetworkMethod.Dryrun)
		{
			if (periodicRequestCoroutine != null)
			{
				StopCoroutine(periodicRequestCoroutine);
			}

			periodicRequestCoroutine = PeriodicRequest(pid, tags, callback, data, method);
			StartCoroutine(periodicRequestCoroutine);
		}

		public void CancelPeriodicRequest()
		{
			if (periodicRequestCoroutine != null)
			{
				StopCoroutine(periodicRequestCoroutine);
				periodicRequestCoroutine = null;
			}
		}

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

		public override void ForceStopAndReset()
		{
			// Cancel any periodic requests first
			CancelPeriodicRequest();

			// Call base class implementation (EnqueueMessageHandler)
			base.ForceStopAndReset();
		}
	}
}