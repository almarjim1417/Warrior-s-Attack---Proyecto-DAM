using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Ajustes de Posición")]
    public float xOffset = 2f;  // Adelanto de cámara
    public float yOffset = 1f;  // Altura extra

    [Header("Límites")]
    public float minX = 0f;    
    public float minY = 0f;    

    [Header("Suavizado")]
    public float smoothSpeed = 0.125f; // Velocidad a la que se mueve la cámara

    void LateUpdate()
    {
        if (target != null)
        {
            // Posición dónde queremos que esté la cámara
            Vector3 desiredPosition = new Vector3(
                target.position.x + xOffset,
                target.position.y + yOffset,
                transform.position.z
            );

            // Límites
            float clampedX = Mathf.Clamp(desiredPosition.x, minX, 100000f);
            float clampedY = Mathf.Clamp(desiredPosition.y, minY, 100000f);

            Vector3 finalPosition = new Vector3(clampedX, clampedY, desiredPosition.z);

            // Lerp mueve la cámara poco a poco
            transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);
        }
    }
}