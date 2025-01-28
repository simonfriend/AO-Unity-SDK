using UnityEngine;
using UnityEngine.EventSystems;

namespace Permaverse.AO
{
	public class HoverPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		public GameObject panelToShow;
		private bool isHovering = false;

		void Start()
		{
			HidePanel();
		}

		// This method is called when the mouse pointer enters the UI object
		public void OnPointerEnter(PointerEventData eventData)
		{
			isHovering = true;
			ShowPanel();
		}

		// This method is called when the mouse pointer exits the UI object
		public void OnPointerExit(PointerEventData eventData)
		{
			isHovering = false;
			HidePanel();
		}

		// This method is called when the UI object is clicked or tapped
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!isHovering) // If not already hovering (for mobile devices)
			{
				// Toggle panel visibility
				if (panelToShow.activeSelf)
					HidePanel();
				else
					ShowPanel();
			}
		}

		// Method to show the panel
		private void ShowPanel()
		{
			panelToShow.SetActive(true); // Show the panel
		}

		// Method to hide the panel
		private void HidePanel()
		{
			panelToShow.SetActive(false); // Hide the panel
		}
	}
}