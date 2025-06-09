using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Permaverse.AO
{
    [RequireComponent(typeof(TMP_Text))]
    public class LinkHandlerTMP : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
		public bool underlineOnHover = false;
        private TMP_Text _tmpTextBox;
        private Canvas _canvasToCheck;
        private Camera _cameraToUse;
        private int _lastHoveredLink = -1;
		private string text;

        public static event Action<string> ClickedOnLink;

        private void Awake()
        {
            _tmpTextBox = GetComponent<TMP_Text>();
            _canvasToCheck = GetComponentInParent<Canvas>();

            if (_canvasToCheck.renderMode == RenderMode.ScreenSpaceOverlay)
                _cameraToUse = null;
            else
                _cameraToUse = _canvasToCheck.worldCamera;

			text = _tmpTextBox.text;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector3 mousePosition = new Vector3(eventData.position.x, eventData.position.y, 0);
            var linkTaggedText = TMP_TextUtilities.FindIntersectingLink(_tmpTextBox, mousePosition, _cameraToUse);

            if (linkTaggedText == -1) return;

            TMP_LinkInfo linkInfo = _tmpTextBox.textInfo.linkInfo[linkTaggedText];
            string linkID = linkInfo.GetLinkID();
            if (linkID.StartsWith("http://") || linkID.StartsWith("https://"))
            {
                Application.OpenURL(linkID);
                return;
            }

            ClickedOnLink?.Invoke(linkInfo.GetLinkText());
        }

        public void OnPointerMove(PointerEventData eventData)
        {
			if (!underlineOnHover) return;

            Vector3 mousePosition = new Vector3(eventData.position.x, eventData.position.y, 0);
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_tmpTextBox, mousePosition, _cameraToUse);

            if (linkIndex != _lastHoveredLink)
            {
                if (_lastHoveredLink != -1)
                    RemoveUnderline(_lastHoveredLink);

                if (linkIndex != -1)
                {
                    AddUnderline(linkIndex);
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // You can set a custom cursor here if you want
                }
                else
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                }

                _lastHoveredLink = linkIndex;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
			if (!underlineOnHover) return;
			
            if (_lastHoveredLink != -1)
            {
                RemoveUnderline(_lastHoveredLink);
                _lastHoveredLink = -1;
            }
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void AddUnderline(int linkIndex)
        {
            var linkInfo = _tmpTextBox.textInfo.linkInfo[linkIndex];
            int startIdx = linkInfo.linkTextfirstCharacterIndex;
            int length = linkInfo.linkTextLength;

            _tmpTextBox.text = $"<u>{text}</u>";
        }

        private void RemoveUnderline(int linkIndex)
        {
            // To keep it simple, just reset the text (if you want to optimize, you can track and remove only the underline tags)
            _tmpTextBox.text = text;
        }
    }
}