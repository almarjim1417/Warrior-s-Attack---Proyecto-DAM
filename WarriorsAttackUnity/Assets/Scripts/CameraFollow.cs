using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Configuración Base")]
    public float xOffset = 2f;
    public float defaultYOffset = 1f;
    public float defaultZoom = 7f;
    public float followSpeed = 3f;
    public float zoomSpeed = 2f;

    [Header("Limites Mapa")]
    public bool enableLimits = true;
    public float minX = -10f;
    public float maxX = 50f;
    public float minY = -3f;

    // Variables internas
    private float currentYOffset;
    private float currentZoom;
    private bool useFixedY = false;
    private float fixedYPosition = 0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        ResetCamera();

        if (cam != null) cam.orthographicSize = defaultZoom;
    }

    // Usamos LateUpdate para que la cámara se mueva después del jugador y no vibre
    void LateUpdate()
    {
        if (target == null) return;

        // Calculamos la posición X (Horizontal)
        float targetX = target.position.x + xOffset;
        if (enableLimits)
        {
            // Clamp evita que la cámara se salga de los límites min y max
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        // Calculamos la posición Y (Vertical)
        float targetY;
        if (useFixedY)
        {
            targetY = fixedYPosition; // Altura fija (para zonas especiales)
        }
        else
        {
            targetY = target.position.y + currentYOffset;
            if (enableLimits && targetY < minY) targetY = minY; // No bajar del suelo
        }

        // Mover la cámara suavemente hacia el destino
        Vector3 destination = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, destination, followSpeed * Time.deltaTime);

        // Aplicar zoom suave
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentZoom, zoomSpeed * Time.deltaTime);
    }

    public void EnterZone(float newZoom, float newHeight, bool lockY)
    {
        currentZoom = newZoom;
        useFixedY = lockY;

        if (lockY) fixedYPosition = newHeight;
        else currentYOffset = newHeight;
    }

    public void ExitZone()
    {
        ResetCamera();
    }

    private void ResetCamera()
    {
        currentZoom = defaultZoom;
        currentYOffset = defaultYOffset;
        useFixedY = false;
    }
}