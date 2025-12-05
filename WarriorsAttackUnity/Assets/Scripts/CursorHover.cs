using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Ponemos el cursor de texto
        FirebaseManager.Instance.SetTextCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reseteamos el cursor al cursor por defecto
        FirebaseManager.Instance.SetDefaultCursor();
    }
}