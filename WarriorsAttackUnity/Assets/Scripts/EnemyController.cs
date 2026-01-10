using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType { Zombie, Stalker }

    [Header("Tipo de Enemigo")]
    public EnemyType enemyType;

    [Header("Vida y Daño")]
    public int maxHealth = 3;
    public int damage = 1;
    private int currentHealth;

    [Header("Configuración Zombie (Patrulla)")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 3f;
    public Transform wallCheckPoint; // Punto desde donde miramos si hay pared
    public float wallCheckDistance = 0.5f;
    public LayerMask whatIsGround; // Qué capas cuentan como suelo/pared

    [Header("Configuración Stalker (Perseguidor)")]
    public float chaseSpeed = 5f;
    public float aggroRange = 6f; // A qué distancia nos ve
    public float attackRange = 1.5f; // A qué distancia pega
    public Transform ledgeCheckPoint; // Punto para mirar si hay precipicio
    public bool avoidFalls = true; // Si es true, no se tirará por huecos
    public Transform player;

    [Header("Combate")]
    public float timeToHit = 0.3f; // Retraso para que el daño coincida con la animación
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool isDead = false;

    [Header("Sonidos")]
    public AudioClip sound_ZombieAttack;
    public AudioClip sound_ZombieHurt;
    public AudioClip sound_StalkerDetect;

    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    private bool movingRight = true;
    private Vector2 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
        startPosition = transform.position;

        // Buscamos al jugador automáticamente si no lo hemos puesto a mano
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (isDead) return;

        // Si estamos atacando, nos quedamos quietos
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Ejecutamos la lógica según qué bicho sea
        switch (enemyType)
        {
            case EnemyType.Zombie:
                ZombieLogic();
                break;
            case EnemyType.Stalker:
                StalkerLogic();
                break;
        }

        // Animación de andar
        anim.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        anim.SetBool("IsChasing", isChasing);
    }

    // --- ZOMBIE: Camina de un lado a otro ---
    void ZombieLogic()
    {
        isChasing = false;

        float speed = movingRight ? patrolSpeed : -patrolSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        // Si nos alejamos mucho del punto de inicio, damos la vuelta
        float distanceFromStart = transform.position.x - startPosition.x;
        if (movingRight && distanceFromStart > patrolDistance) Flip();
        else if (!movingRight && distanceFromStart < -patrolDistance) Flip();

        // Si nos chocamos con una pared, damos la vuelta
        if (wallCheckPoint != null)
        {
            bool hitWall = Physics2D.Raycast(wallCheckPoint.position, transform.right, wallCheckDistance, whatIsGround);
            if (hitWall) Flip();
        }
    }

    // --- STALKER: Persigue al jugador ---
    void StalkerLogic()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isChasing)
        {
            // Está quieto esperando
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Si el jugador se acerca, empieza la persecución
            if (distanceToPlayer < aggroRange)
            {
                isChasing = true;
                if (audioSource != null && sound_StalkerDetect != null)
                    audioSource.PlayOneShot(sound_StalkerDetect);
            }
        }
        else
        {
            // Ya nos ha visto
            if (distanceToPlayer <= attackRange)
            {
                // Está a rango de golpe: Ataca
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(PerformAttackSequence(player.gameObject));
            }
            else
            {
                // Está lejos: Persigue

                // Evitar caídas por precipicios
                if (avoidFalls && ledgeCheckPoint != null)
                {
                    bool haySuelo = Physics2D.Raycast(ledgeCheckPoint.position, Vector2.down, 2f, whatIsGround);
                    if (!haySuelo)
                    {
                        // Si no hay suelo, se frena y no avanza
                        rb.linearVelocity = Vector2.zero;
                        return;
                    }
                }

                ChasePlayer();
            }
        }
    }

    void ChasePlayer()
    {
        // Miramos hacia el jugador
        if (transform.position.x < player.position.x && !movingRight) Flip();
        else if (transform.position.x > player.position.x && movingRight) Flip();

        float speed = movingRight ? chaseSpeed : -chaseSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    void Flip()
    {
        movingRight = !movingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (audioSource != null && sound_ZombieHurt != null)
            audioSource.PlayOneShot(sound_ZombieHurt);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");
            // Si le pegamos, nos empieza a perseguir aunque fuera un Zombie tranquilo
            if (enemyType == EnemyType.Stalker) isChasing = true;
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetBool("IsDead", true);

        // Sumar Kill al jugador y a Firebase
        if (player != null)
        {
            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null) playerScript.RegistrarKill();
        }

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.ActualizarEstadistica("kill", 1);
        }

        // Desactivamos el script y ajustamos el colider para que se pueda pisar el cadáver
        this.enabled = false;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(1f, 0.2f);
            col.offset = new Vector2(0f, -0.5f);
            col.isTrigger = true;
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Quitamos físicas
        gameObject.tag = "Untagged"; // Quitamos etiqueta de enemigo

        Destroy(gameObject, 2f); // Desaparece a los 2 segundos
    }

    // Para cuando el Zombie choca con el jugador simplemente caminando
    void OnCollisionStay2D(Collision2D collision)
    {
        if (enemyType == EnemyType.Zombie && !isDead && !isAttacking)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                // Nos giramos para mirarle y atacamos
                float direccion = collision.transform.position.x - transform.position.x;
                if ((direccion > 0 && !movingRight) || (direccion < 0 && movingRight)) Flip();

                StartCoroutine(PerformAttackSequence(collision.gameObject));
            }
        }
    }

    IEnumerator PerformAttackSequence(GameObject target)
    {
        isAttacking = true;
        anim.SetTrigger("Attack");

        if (audioSource != null && sound_ZombieAttack != null)
            audioSource.PlayOneShot(sound_ZombieAttack);

        // Esperamos el momento justo de la animación para hacer daño
        yield return new WaitForSeconds(timeToHit);

        if (target != null && !isDead)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            // Comprobamos si el jugador sigue cerca
            if (dist <= attackRange + 1f)
            {
                PlayerController playerScript = target.GetComponent<PlayerController>();
                if (playerScript != null) playerScript.TakeDamage(damage, transform);
            }
        }

        // Pequeña pausa después de atacar
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    // Dibujitos en el editor para ver los rangos
    void OnDrawGizmosSelected()
    {
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ledgeCheckPoint.position, ledgeCheckPoint.position + Vector3.down * 2f);
        }

        if (enemyType == EnemyType.Stalker)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRange); // Rango visión
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange); // Rango ataque
        }
    }
}