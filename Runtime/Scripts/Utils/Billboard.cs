using UnityEngine;

namespace Permaverse.AO
{
	public class Billboard : MonoBehaviour
	{
		public bool rotateOnlyY = false; // Set to true to rotate only on the Y-axis

		private Transform camTransform;

		void Start()
		{
			// Find the main camera
			if (Camera.main != null)
				camTransform = Camera.main.transform;
			else
				Debug.LogError("No main camera found. Ensure there is a camera in the scene.");
		}

		void Update()
		{
			if (camTransform != null)
			{
				// Always face the camera
				transform.LookAt(transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);

				// If rotateOnlyY is true, reset rotation on X and Z axes
				if (rotateOnlyY)
				{
					Vector3 eulerAngles = transform.eulerAngles;
					eulerAngles.x = 0f;
					eulerAngles.z = 0f;
					transform.eulerAngles = eulerAngles;
				}
			}
		}
	}
}