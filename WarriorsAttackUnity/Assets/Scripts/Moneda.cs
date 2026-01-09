using UnityEngine;

public class Moneda : MonoBehaviour
{
    public int valor = 1;
    public GameObject pickupVFX;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Optimizacion: Primero comprobamos el Tag para no hacer GetComponents innecesarios
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                player.RecogerMoneda(valor);

                if (pickupVFX != null)
                {
                    Instantiate(pickupVFX, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
            }
        }
    }
}