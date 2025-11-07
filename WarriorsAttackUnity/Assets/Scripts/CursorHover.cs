using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D cursorTexto;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Vector2.zero es el lugar donde apunta el cursor.
        Vector2 hotspot = new Vector2(cursorTexto.width / 2, cursorTexto.height / 2);
        Cursor.SetCursor(cursorTexto, hotspot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reseteamos el cursor al cursor por defecto
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}