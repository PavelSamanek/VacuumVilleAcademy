using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VacuumVille.UI
{
    /// <summary>
    /// Exposes pointer-down/up events as UnityEvents so they can be wired in the
    /// Inspector or via code (used by ParentDashboardController PIN gate).
    /// </summary>
    public class EventTriggerHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent onPointerDown = new();
        public UnityEvent onPointerUp   = new();

        public void OnPointerDown(PointerEventData e) => onPointerDown.Invoke();
        public void OnPointerUp(PointerEventData e)   => onPointerUp.Invoke();
    }
}
