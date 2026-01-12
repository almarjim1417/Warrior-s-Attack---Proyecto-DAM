using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Estadísticas")]
    public float speed = 10f;
    public float lifeTime = 4f;
    public int damage = 1;

    [Header("Efectos Visuales")]
    public GameObject impactVFX; // Partículas

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Esta función la llama el BossController cuando dispara la roca
    public void Lanzar(Vector2 direction)
    {
        rb.gravityScale = 0f; // Quitamos la gravedad para que vuele recto y a la misma velocidad y dirección
        rb.linearVelocity = direction.normalized * speed;

        // Convertimos la diagonal en un ángulo, lo pasamos a grados y aplicamos el giro.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Solo entramos si lo que tocamos no es enemigo
        if (!hitInfo.CompareTag("Enemy") && hitInfo.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            bool haChocado = false;

            PlayerController player = hitInfo.GetComponent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(damage, transform);
                haChocado = true;
            }
            else if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                haChocado = true;
            }

            if (haChocado)
            {
                SpawnImpactEffect();
                Destroy(gameObject);
            }
        }
    }

    void SpawnImpactEffect()
    {
        if (impactVFX != null)
        {
            // Creamos las partículas de explosión en el lugar de la roca
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }
    }
}