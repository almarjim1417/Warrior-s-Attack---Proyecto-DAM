using UnityEngine;

public class Moneda : MonoBehaviour
{
    public int valor = 1;
    public GameObject pickupVFX;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo actúa si el player es quién entra en contacto
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                player.RecogerMoneda(valor);

                if (pickupVFX != null)
                {
                    // Crea la animación
                    Instantiate(pickupVFX, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
            }
        }
    }
}