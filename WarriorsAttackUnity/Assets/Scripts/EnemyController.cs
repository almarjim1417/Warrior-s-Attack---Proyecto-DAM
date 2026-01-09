using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType { Zombie, Stalker }
    public EnemyType enemyType;

    [Header("Estadísticas Comunes")]
    public int maxHealth = 3;
    private int currentHealth;
    public int damage = 1;
    private Rigidbody2D rb;
    private Animator anim;
    private bool movingRight = true;
    private bool isDead = false;

    [Header("Configuración ZOMBIE")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 3f;
    private Vector2 startPosition;
    public Transform wallCheckPoint;
    public float wallCheckDistance = 0.5f;
    public LayerMask whatIsGround;

    [Header("Configuración STALKER")]
    public float chaseSpeed = 5f;
    public float aggroRange = 6f;
    public float attackRange = 1.5f;
    public Transform player;

    [Header("Detección de Caídas (Stalker)")]
    public Transform ledgeCheckPoint;
    public bool avoidFalls = true;

    [Header("Combate")]
    public float timeToHit = 0.3f;
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool isBlocked = false;

    [Header("Audio (Arrastra tus archivos aquí)")]
    public AudioClip sound_ZombieAttack; 
    public AudioClip sound_ZombieHurt; 
    public AudioClip sound_StalkerDetect; 

    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
        startPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (isDead) return;
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (enemyType == EnemyType.Zombie) ZombieLogic();
        else if (enemyType == EnemyType.Stalker) StalkerLogic();

        anim.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        anim.SetBool("IsChasing", isChasing && !isBlocked);
    }

    void ZombieLogic()
    {
        isChasing = false;

        float speed = movingRight ? patrolSpeed : -patrolSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        float distanceFromStart = transform.position.x - startPosition.x;
        if (movingRight && distanceFromStart > patrolDistance) Flip();
        else if (!movingRight && distanceFromStart < -patrolDistance) Flip();

        if (wallCheckPoint != null)
        {
            bool hitWall = Physics2D.Raycast(wallCheckPoint.position, transform.right, wallCheckDistance, whatIsGround);
            if (hitWall) Flip();
        }
    }

    void StalkerLogic()
    {
        if (player == null) return;

        if (!player.CompareTag("Player"))
        {
            rb.linearVelocity = Vector2.zero;
            isChasing = false;
            anim.SetBool("IsMoving", false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isChasing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Si entra en rango, empieza a perseguir
            if (distanceToPlayer < aggroRange)
            {
                isChasing = true;

                // --- SONIDO DETECTAR (Solo Stalker) ---
                if (audioSource != null && sound_StalkerDetect != null)
                    audioSource.PlayOneShot(sound_StalkerDetect);
            }
        }
        else
        {
            if (distanceToPlayer <= attackRange)
            {
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(PerformAttackSequence(player.gameObject));
            }
            else
            {
                isBlocked = false;

                if (avoidFalls && ledgeCheckPoint != null)
                {
                    bool isThereGround = Physics2D.Raycast(ledgeCheckPoint.position, Vector2.down, 2f, whatIsGround);

                    if (!isThereGround)
                    {
                        rb.linearVelocity = Vector2.zero;
                        isBlocked = true;

                        if (transform.position.x < player.position.x && !movingRight) Flip();
                        else if (transform.position.x > player.position.x && movingRight) Flip();

                        return;
                    }
                }

                ChasePlayer();
            }
        }
    }

    void ChasePlayer()
    {
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

        // --- SONIDO HURT ---
        if (audioSource != null && sound_ZombieHurt != null)
            audioSource.PlayOneShot(sound_ZombieHurt);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");
            if (enemyType == EnemyType.Stalker) isChasing = true;
        }
    }

    void Die()
    {
        isDead = true;
        anim.SetBool("IsDead", true);

        if (player != null)
        {
            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                playerScript.RegistrarKill();
            }
        }

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.ActualizarEstadistica("kill", 1);
        }

        this.enabled = false;

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(1f, 0.2f);
            col.offset = new Vector2(0f, -0.5f);
            col.isTrigger = true;
        }

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        gameObject.tag = "Untagged";
        Destroy(gameObject, 2f);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (enemyType == EnemyType.Zombie && !isDead && !isAttacking)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                float direccionJugador = collision.transform.position.x - transform.position.x;

                if (direccionJugador > 0 && !movingRight) Flip();
                else if (direccionJugador < 0 && movingRight) Flip();

                StartCoroutine(PerformAttackSequence(collision.gameObject));
            }
        }
    }

    IEnumerator PerformAttackSequence(GameObject target)
    {
        isAttacking = true;
        anim.SetTrigger("Attack");

        // --- SONIDO ATAQUE ---
        if (audioSource != null && sound_ZombieAttack != null)
            audioSource.PlayOneShot(sound_ZombieAttack);

        yield return new WaitForSeconds(timeToHit);

        if (target != null && !isDead)
        {
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist <= attackRange + 1f)
            {
                PlayerController playerScript = target.GetComponent<PlayerController>();
                if (playerScript != null) playerScript.TakeDamage(damage, transform);
            }
        }
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(ledgeCheckPoint.position, ledgeCheckPoint.position + Vector3.down * 1f);
        }

        if (enemyType == EnemyType.Stalker)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}