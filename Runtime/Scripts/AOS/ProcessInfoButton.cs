using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
	public class ProcessInfoButton : MonoBehaviour
	{
		public AOProcess process;

		[Header("UI")]
		[SerializeField] private TMP_Text processName;
		[SerializeField] private TMP_Text processID;
		[SerializeField] private Button button;
		[SerializeField] private Image border;

		[SerializeField] private Color highlightColor = Color.green;

		private void Start()
		{
			button.onClick.AddListener(LoadProcess);
		}

		public void SetInfo(AOProcess process)
		{
			this.process = process;
			processName.text = process.name;
			processID.text = process.shortId;
		}

		public void ToggleHighlight(bool setActive)
		{
			if (setActive)
			{
				border.color = highlightColor;
			}
			else
			{
				border.color = Color.white;
			}
		}

		public void LoadProcess()
		{
			ProcessHandler.main.LoadProcess(process);
			ToggleHighlight(true);
		}
	}
}