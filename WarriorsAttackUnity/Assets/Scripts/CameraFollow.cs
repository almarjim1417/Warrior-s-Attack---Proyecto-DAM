using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Base Settings")]
    public float xOffset = 2f;
    public float defaultYOffset = 1f;
    public float defaultZoom = 7f;
    public float followSpeed = 3f;
    public float zoomSpeed = 2f;

    [Header("Map Boundaries")]
    public bool enableLimits = true;
    public float minX = -10f;
    public float maxX = 50f;
    public float minY = -3f;

    // Internal State
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

    // Usamos LateUpdate para mover la cámara DESPUÉS de que el jugador se haya movido
    void LateUpdate()
    {
        if (target == null) return;

        // 1. Calculate X
        float targetX = target.position.x + xOffset;
        if (enableLimits)
        {
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        // 2. Calculate Y
        float targetY;
        if (useFixedY)
        {
            targetY = fixedYPosition;
        }
        else
        {
            targetY = target.position.y + currentYOffset;
            if (enableLimits && targetY < minY) targetY = minY;
        }

        // 3. Smooth Move & Zoom
        Vector3 destination = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, destination, followSpeed * Time.deltaTime);

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