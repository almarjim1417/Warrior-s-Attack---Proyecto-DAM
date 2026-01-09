using UnityEngine;

public class SpearController : MonoBehaviour
{
    [Header("Configuración")]
    public float speed = 15f;
    public float lifeTime = 3f; // Tiempo antes de desaparecer si no choca
    public int damage = 1;

    [Header("Efectos")]
    public GameObject impactoVFX;
    public float puntaOffset = 0.8f;
    public AnimationClip animacionLanza;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = transform.right * speed;

        // Autodestrucción: Si tiene animación usamos su duración, si no, el tiempo fijo
        float tiempoVida = (animacionLanza != null) ? animacionLanza.length : lifeTime;
        Destroy(gameObject, tiempoVida);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Player") || hitInfo.CompareTag("Monedas")) return;

        // También ignoramos triggers invisibles (como zonas de cámara)
        if (hitInfo.isTrigger) return;

        // 1. ¿Hemos dado a un enemigo normal?
        EnemyController enemy = hitInfo.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        BossController boss = hitInfo.GetComponent<BossController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
        }

        if (impactoVFX != null)
        {
            // Calculamos la posición de la punta sumando un poco en la dirección de movimiento
            Vector3 posicionPunta = transform.position + (transform.right * puntaOffset);
            Instantiate(impactoVFX, posicionPunta, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}