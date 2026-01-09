using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public float lifeTime = 4f;
    public int damage = 1;

    [Header("VFX")]
    public GameObject impactVFX;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Se llama desde el BossController al instanciar la roca
    public void Lanzar(Vector2 direction)
    {
        rb.gravityScale = 0f; // Anulamos gravedad para trayectoria recta
        rb.linearVelocity = direction.normalized * speed;

        // Orientamos el sprite hacia la dirección del movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Autodestrucción por tiempo
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. Ignorar fuego amigo (Boss o Enemigos)
        if (hitInfo.CompareTag("Enemy") || hitInfo.gameObject.layer == LayerMask.NameToLayer("Enemy")) return;

        bool hasHit = false;

        // 2. Comprobar si golpea al Jugador
        PlayerController player = hitInfo.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage, transform);
            hasHit = true;
        }
        // 3. Comprobar si golpea el Suelo
        else if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasHit = true;
        }

        // 4. Si ha chocado con algo válido: Efecto y Destrucción
        if (hasHit)
        {
            SpawnImpactEffect();
            Destroy(gameObject);
        }
    }

    void SpawnImpactEffect()
    {
        if (impactVFX != null)
        {
            Instantiate(impactVFX, transform.position, Quaternion.identity);
        }
    }
}