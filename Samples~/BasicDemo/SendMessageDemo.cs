using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
    public class SendMessageDemo : MonoBehaviour
    {
        public TMP_InputField inputFieldPid;
        public TMP_InputField inputFieldData;
        public TMP_InputField inputFieldAction;
        public Button sendMessageButton;
        public Button sendDryrunButton;

        public TMP_Text responseText;

        public MessageHandler messageHandler;

        public void Start()
        {
            //! In editor we cannot send messages, only dryruns
            if(Application.isEditor && sendMessageButton != null)
            {
                sendMessageButton.interactable = false;
            }

            sendMessageButton?.onClick.AddListener(() => SendCustomMessage(MessageHandler.NetworkMethod.Message));
            sendDryrunButton?.onClick.AddListener(() => SendCustomMessage(MessageHandler.NetworkMethod.Dryrun));
        }

        public void SendCustomMessage(MessageHandler.NetworkMethod networkMethod)
        {
            List<Tag> tags = new List<Tag>();
            Tag actionTag = new Tag("Action", inputFieldAction.text);
            tags.Add(actionTag);

            messageHandler.SendRequest(inputFieldPid.text, tags, OnMessageResult, inputFieldData.text, networkMethod);
            responseText.text = $"Sending {(networkMethod == MessageHandler.NetworkMethod.Message ? "message" : "dryrun")} to {inputFieldPid.text} with Action: {inputFieldAction.text} and Data: {inputFieldData.text}";
        }

        public void OnMessageResult(bool result, NodeCU nodeCU)
        {
            Debug.Log($"Result: {result}");
            responseText.text = nodeCU.ToString();            
        }
    }
}