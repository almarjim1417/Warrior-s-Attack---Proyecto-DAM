using UnityEngine;

public class AutodestruirVFX : MonoBehaviour
{
    [Tooltip("Tiempo en segundos antes de destruir el objeto")]
    public float lifetime = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}