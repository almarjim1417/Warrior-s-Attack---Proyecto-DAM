using UnityEngine;

public class ActivadorCamino : MonoBehaviour
{
    public GameObject muroBloqueo;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (muroBloqueo != null)
            {
                muroBloqueo.SetActive(true);
            }

            Destroy(gameObject);
        }
    }
}