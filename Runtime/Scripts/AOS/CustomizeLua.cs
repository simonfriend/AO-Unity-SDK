using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
    public class CustomizeLua : MonoBehaviour
    {
        public Button cancelButton;
        public Button saveButton;

        public string baseName;
        public TMP_InputField customNameInputField;

        public List<CustomizableVariable> variables;

        public GameObject successPanel;

        [TextArea(40, 20)]
        public string originalLuaFile;

        private string tmpFile;

        void Start()
        {
            cancelButton.onClick.AddListener(Cancel);
            saveButton.onClick.AddListener(Save);
        }

        private void OnDisable()
        {
            customNameInputField.text = "";
        }

        private void Cancel()
        {
            gameObject.SetActive(false);
        }

        private void Save()
        {
            if (!string.IsNullOrEmpty(customNameInputField.text))
            {
                //Save
                tmpFile = originalLuaFile;

                foreach (CustomizableVariable variable in variables)
                {
                    tmpFile = tmpFile.Replace(variable.textVariableName, variable.variableValue.text);
                }

                string filePath = Path.Combine(Application.dataPath, "StreamingAssets", baseName + customNameInputField.text + ".lua");

                File.WriteAllText(filePath, tmpFile);

                StartCoroutine(OpenSuccessPanelAndClose());
            }
        }

        private IEnumerator OpenSuccessPanelAndClose()
        {
            successPanel.SetActive(true);

            yield return new WaitForSeconds(2);

            successPanel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}