using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Cambiamos el cursor
        FirebaseManager.Instance?.SetTextCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Volvemos a poner el cursor normal
        FirebaseManager.Instance?.SetDefaultCursor();
    }
}