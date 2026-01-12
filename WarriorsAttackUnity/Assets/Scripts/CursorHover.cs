using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Establecemos el cursor de texto
        FirebaseManager.Instance?.SetTextCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Volvemos a poner el cursor normal
        FirebaseManager.Instance?.SetDefaultCursor();
    }
}