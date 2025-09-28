using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
    /// <summary>
    /// Demo script showcasing AO SDK functionality including messages, dryruns, and HyperBeam paths
    /// </summary>
    public class SendMessageDemo : MonoBehaviour
    {
        [Header("UI Components - Message Testing")]
        public TMP_InputField inputFieldPid;
        public TMP_InputField inputFieldData;
        public TMP_InputField inputFieldAction;
        public Button sendMessageButton;
        public Button sendDryrunButton;

        [Header("UI Components - HyperBeam Testing")]
        public TMP_InputField inputFieldHyperBeamNode;
        public Button sendHyperBeamMessageButton;

        [Header("Output")]
        public TMP_Text responseText;

        [Header("Handlers")]
        public MessageHandler messageHandler;

        public void Start()
        {
            // Setup message testing buttons
            if (Application.isEditor && sendMessageButton != null)
            {
                sendMessageButton.interactable = false;
            }

            sendMessageButton?.onClick.AddListener(() => SendCustomMessage(MessageHandler.NetworkMethod.Message));
            sendDryrunButton?.onClick.AddListener(() => SendCustomMessage(MessageHandler.NetworkMethod.Dryrun));
            sendHyperBeamMessageButton?.onClick.AddListener(() => SendCustomMessage(MessageHandler.NetworkMethod.HyperBeamMessage));

            // Set default values for testing
            if (inputFieldPid != null && string.IsNullOrEmpty(inputFieldPid.text))
                inputFieldPid.text = "your-process-id-here";

            if (inputFieldAction != null && string.IsNullOrEmpty(inputFieldAction.text))
                inputFieldAction.text = "Info";

            if (inputFieldHyperBeamNode != null && string.IsNullOrEmpty(inputFieldHyperBeamNode.text))
                inputFieldHyperBeamNode.text = "http://localhost:8734";
            
        }

        /// <summary>
        /// Send a custom message or dryrun to an AO process
        /// </summary>
        public void SendCustomMessage(MessageHandler.NetworkMethod networkMethod)
        {
            if (messageHandler == null)
            {
                responseText.text = "Error: MessageHandler not assigned!";
                return;
            }

            List<Tag> tags = new List<Tag>();
            if (!string.IsNullOrEmpty(inputFieldAction.text))
            {
                tags.Add(new Tag("Action", inputFieldAction.text));
            }

            messageHandler.SendRequestAsync(inputFieldPid.text, tags, inputFieldData.text, networkMethod, callback: OnMessageResult).Forget();
            responseText.text = $"Sending {(networkMethod == MessageHandler.NetworkMethod.Message ? "message" : "dryrun")} to {inputFieldPid.text} with Action: {inputFieldAction.text} and Data: {inputFieldData.text}";
        }
    
        // Callback handlers
        public void OnMessageResult(bool result, NodeCU nodeCU)
        {
            Debug.Log($"Message Result: {result}");
            responseText.text = $"Message Result: {result}\n\n{nodeCU.ToString()}";            
        }
    }
}