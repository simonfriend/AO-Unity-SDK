using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
    public enum VariableType
    {
        None,
        Int,
        Float,
        String,
    }

    public class CustomizableVariable : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text variableValue;
        public Button editVariableButton;

        [Header("Customizable Values")]
        public VariableType type;
        public string textVariableName;
        public string defaultValue;

        [Header("Optional")]
        public float sliderMult = 1;
        public float minValue;
        public float maxValue;

        void Start()
        {
            editVariableButton.onClick.AddListener(OpenEditPopup);
        }

        private void OnDisable()
        {
            variableValue.text = defaultValue;
        }

        private void OpenEditPopup()
        {
            if (type == VariableType.String)
            {
                AOSManager.main.editVariablePopup.OpenPopup(type, UpdateValues, variableValue.text);
            }
            else
            {
                AOSManager.main.editVariablePopup.OpenPopup(type, UpdateValues, variableValue.text, minValue, maxValue, sliderMult);
            }
        }

        private void UpdateValues(string value, bool edited)
        {
            if (edited)
            {
                variableValue.text = value;
            }
        }
    }
}