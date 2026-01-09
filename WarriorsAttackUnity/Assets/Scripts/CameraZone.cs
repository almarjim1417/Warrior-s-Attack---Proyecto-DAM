using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public float targetZoom = 10f;
    public bool lockY = false;
    public float targetHeight = 0f;

    private CameraFollow camScript;

    void Start()
    {
        if (Camera.main != null)
        {
            camScript = Camera.main.GetComponent<CameraFollow>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (camScript != null && collision.CompareTag("Player"))
        {
            camScript.EnterZone(targetZoom, targetHeight, lockY);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (camScript != null && collision.CompareTag("Player"))
        {
            camScript.ExitZone();
        }
    }
}