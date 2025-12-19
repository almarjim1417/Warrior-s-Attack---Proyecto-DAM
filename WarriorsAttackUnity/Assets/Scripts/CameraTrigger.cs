using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [Header("Nuevo Límite")]
    public float newMinY;

    private void OnTriggerEnter2D(Collider2D objetivo)
    {
        if (objetivo.CompareTag("Player"))
        {
            CameraFollow camScript = Camera.main.GetComponent<CameraFollow>();

            if (camScript != null)
            {
                // Cambiamos el límite
                camScript.minY = newMinY;

            }
        }
    }
}