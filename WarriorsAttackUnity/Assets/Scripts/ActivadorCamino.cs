using UnityEngine;

public class ActivadorCamino : MonoBehaviour
{
    public GameObject muroBloqueo;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Si el que entra es el jugador
        if (collision.CompareTag("Player"))
        {
            if (muroBloqueo != null)
            {
                // Activamos el muro para bloquear el camino
                muroBloqueo.SetActive(true);
            }

            // Destruimos este objeto para que no se vuelva a usar
            Destroy(gameObject);
        }
    }
}