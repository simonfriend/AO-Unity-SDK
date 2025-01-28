using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace Permaverse.AO
{
    public class HoverableButton : Button, IPointerEnterHandler, IPointerExitHandler
    {
        // Events for hover enter and hover exit
        public event Action<HoverableButton> OnHoverEnter;
        public event Action<HoverableButton> OnHoverExit;

        // Handle pointer enter
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData); // Call the base method to retain normal button behavior
            if (interactable) // Only trigger hover events if the button is interactable
            {
                OnHoverEnter?.Invoke(this);
            }
        }

        // Handle pointer exit
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData); // Call the base method to retain normal button behavior
            if (interactable) // Only trigger hover events if the button is interactable
            {
                OnHoverExit?.Invoke(this);
            }
        }

        // Handle button click with inherited functionality
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData); // Call the base method to retain normal button behavior
        }
    }
}