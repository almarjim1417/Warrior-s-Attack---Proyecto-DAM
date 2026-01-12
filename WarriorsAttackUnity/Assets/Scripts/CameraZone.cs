using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("Configuracion")]
    public float targetZoom = 10f;
    public float heightOffset = 2f;
    public bool lockYPosition = false;

    private CameraFollow cam;

    void Start()
    {
        cam = Camera.main.GetComponent<CameraFollow>();
    }


    // Si el jugador entra en la zona cambia los ajustes de la cámara
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && cam != null)
        {
            cam.EnterZone(targetZoom, heightOffset, lockYPosition);
        }
    }


    // Al salir, se resetea
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && cam != null)
        {
            cam.ExitZone();
        }
    }
}