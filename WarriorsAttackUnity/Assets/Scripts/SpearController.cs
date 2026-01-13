using UnityEngine;

public class SpearController : MonoBehaviour
{
    [Header("Ajustes")]
    public float speed = 15f;
    public float lifeTime = 3f;
    public int damage = 1;

    [Header("Efectos Visuales")]
    public GameObject impactoVFX;
    public float puntaOffset = 0.8f; // Desviación para que la explosión salga en la punta
    public AnimationClip animacionLanza;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Disparamos la lanza hacia adelante
        rb.linearVelocity = transform.right * speed;

        float tiempo = (animacionLanza != null) ? animacionLanza.length : lifeTime;
        Destroy(gameObject, tiempo);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (!hitInfo.CompareTag("Player") && !hitInfo.CompareTag("Monedas") && !hitInfo.isTrigger)
        {
            // Comprobar si golpea a un Enemigo
            EnemyController enemy = hitInfo.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);

            // Comprobar si golpea al Boss
            BossController boss = hitInfo.GetComponent<BossController>();
            if (boss != null) boss.TakeDamage(damage);

            // Crear efecto visual de impacto
            if (impactoVFX != null)
            {
                // Calculamos la posición exacta de la punta
                Vector3 pos = transform.position + (transform.right * puntaOffset);
                Instantiate(impactoVFX, pos, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}