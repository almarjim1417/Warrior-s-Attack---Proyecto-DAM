using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("Configuracion")]
    public float targetZoom = 10f;
    public float heightOffset = 2f;
    public bool lockYPosition = false; // Si activas esto, la camara no sube ni baja

    private CameraFollow cam;

    void Start()
    {
        cam = Camera.main.GetComponent<CameraFollow>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && cam != null)
        {
            // Al entrar, le decimos a la camara que use estos ajustes
            cam.EnterZone(targetZoom, heightOffset, lockYPosition);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && cam != null)
        {
            // Al salir, que vuelva a la normalidad
            cam.ExitZone();
        }
    }
}