using UnityEngine;

public class AutodestruirVFX : MonoBehaviour
{
    // Tiempo en segundos que dura el efecto en pantalla
    public float lifetime = 1f;

    void Start()
    {
        // Destruimos el objeto, una vez termine la animación
        Destroy(gameObject, lifetime);
    }
}