using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        FirebaseManager.Instance?.SetTextCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FirebaseManager.Instance?.SetDefaultCursor();
    }
}