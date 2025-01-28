using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
	public class EditVariablePopup : MonoBehaviour
	{
		public VariableType variableType;

		public Slider slider;
		public TMP_Text sliderValueText;
		public TMP_InputField inputField;

		public Button confirmButton;
		public Button cancelButton;

		private Action<string, bool> onConfirm;
		private float sliderMultiplier = 1;

		void Start()
		{
			confirmButton.onClick.AddListener(() => ClosePopup(true));
			cancelButton.onClick.AddListener(() => ClosePopup(false));

			slider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		public void OpenPopup(VariableType type, Action<string, bool> callback, string value, float minValue = -1, float maxValue = -1, float sliderMult = 1)
		{
			gameObject.SetActive(true);

			variableType = type;
			onConfirm = callback;

			if (variableType == VariableType.String)
			{
				inputField.text = value;
				inputField.gameObject.SetActive(true);
			}
			else
			{
				if (variableType == VariableType.Int)
				{
					slider.wholeNumbers = true;
				}
				else
				{
					slider.wholeNumbers = false;
				}

				slider.minValue = minValue;
				slider.maxValue = maxValue;
				sliderMultiplier = sliderMult;
				slider.value = float.Parse(value) / sliderMultiplier;
				slider.gameObject.SetActive(true);
				sliderValueText.text = "Current value: " + (slider.value * sliderMultiplier).ToString();
			}
		}

		public void ClosePopup(bool confirm)
		{
			switch (variableType)
			{
				case VariableType.None:
					Debug.LogError("Variable type shouldn't be None!");
					break;
				case VariableType.Int:
					onConfirm?.Invoke((slider.value * sliderMultiplier).ToString(), confirm);
					break;
				case VariableType.Float:
					onConfirm?.Invoke((slider.value * sliderMultiplier).ToString("F1"), confirm);
					break;
				case VariableType.String:
					onConfirm?.Invoke(inputField.text, confirm);
					break;
			}

			gameObject.SetActive(false);
			slider.gameObject.SetActive(false);
			inputField.gameObject.SetActive(false);

			onConfirm = null;
		}

		private void OnSliderValueChanged(float value)
		{
			if (variableType == VariableType.Float)
			{
				sliderValueText.text = "Current value: " + (sliderMultiplier * value).ToString("F1");
			}
			else
			{
				sliderValueText.text = "Current value: " + (sliderMultiplier * value).ToString();
			}
		}
	}
}