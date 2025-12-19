using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Selector para el tipo de enemigo
    public enum EnemyType { Zombie, Stalker }
    public EnemyType enemyType;

    [Header("Estadísticas")]
    public float patrolSpeed = 2f;    // Velocidad Zombie
    public float chaseSpeed = 6f;     // Velocidad Stalker
    public int maxHealth = 3;
    private int currentHealth;


    [Header("Detección")]
    public float aggroRange = 5f;     // A qué distancia nos detecta
    public Transform player;

    [Header("Patrulla (Configuración)")]
    public float patrolDistance = 3f;
    private Vector2 startPosition;    // Donde nacío el zombie

    [Header("Choques (Paredes reales)")]
    public Transform wallCheckPoint;
    public float wallCheckDistance = 0.5f;
    public LayerMask whatIsGround;

    [Header("Combate")]
    public int damage = 1;
    public float attackRate = 1.5f; // Tiempo entre ataques
    private float nextAttackTime = 0f;

    public float timeToHit = 0.3f; // Tiempo desde que inicia la anim hasta que Muerde

    private Rigidbody2D rb;
    private Animator anim;
    private bool movingRight = true;
    private bool isChasing = false;   // Comprobamos si nos ha detectado
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // Inicializamos el animator
        currentHealth = maxHealth;

        startPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (currentHealth <= 0) return;

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("IsMoving", false);
            return;
        }

        if (enemyType == EnemyType.Stalker)
        {
            CheckForPlayer(); // El Acechador busca al jugador
        }

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        // Actualizamos si está andando para cambiar la animación
        anim.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
    }

    void Patrol()
    {
        // Moverse
        float speed = movingRight ? patrolSpeed : -patrolSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        float distanceFromStart = transform.position.x - startPosition.x;

        if (movingRight && distanceFromStart > patrolDistance)
        {
            Flip();
        }
        else if (!movingRight && distanceFromStart < -patrolDistance)
        {
            Flip();
        }

        // Si se topa con algún objeto
        bool hitWall = Physics2D.Raycast(wallCheckPoint.position, transform.right, wallCheckDistance, whatIsGround);
        if (hitWall)
        {
            Flip();
        }

    }

    void CheckForPlayer()
    {
        if (player == null) return;

        // Calculamos la distancia entre el enemigo y el pj
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer < aggroRange)
        {
            isChasing = true;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        // Correr hacia la dcha o izqda según Lincoln
        if (transform.position.x < player.position.x)
        {
            rb.linearVelocity = new Vector2(chaseSpeed, rb.linearVelocity.y);
            if (!movingRight) Flip();
        }
        else
        {
            rb.linearVelocity = new Vector2(-chaseSpeed, rb.linearVelocity.y);
            if (movingRight) Flip();
        }
    }

    void Flip()
    {
        movingRight = !movingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Activamos el trigger de daño
        anim.SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

void Die()
    {
        anim.SetBool("IsDead", true);
        this.enabled = false;

        // 1. CONFIGURAR LA CAJA (Pequeña y FANTASMA)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = new Vector2(1f, 0.2f);    
            col.offset = new Vector2(0f, 0.1f); 
            col.isTrigger = true; // <--- AQUÍ ESTÁ LA MAGIA: Ahora se atraviesa
        }

        // 2. CONGELARLO EN EL SITIO (Para que no se caiga al ser fantasma)
        rb.gravityScale = 0;             // Apagamos la gravedad
        rb.linearVelocity = Vector2.zero;      // Frenamos en seco
        rb.bodyType = RigidbodyType2D.Kinematic; // Lo convertimos en un objeto "estático"

        // 3. Quitamos etiquetas
        gameObject.tag = "Untagged";
        
        // 4. Adiós a los 2 segundos
        Destroy(gameObject, 2f);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextAttackTime)
            {
                Attack(collision.gameObject);
            }
        }
    }

    void Attack(GameObject target)
    {
        if (target.transform.position.x < transform.position.x && movingRight) Flip();
        else if (target.transform.position.x > transform.position.x && !movingRight) Flip();

        nextAttackTime = Time.time + attackRate;

        StartCoroutine(PerformAttackSequence(target));
    }

    IEnumerator PerformAttackSequence(GameObject target)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(timeToHit);

        if (target != null)
        {
            PlayerController playerScript = target.GetComponent<PlayerController>();

            if (playerScript != null)
            {
                playerScript.TakeDamage(damage, transform);
            }
        }

        // Espera para terminar la animación
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
    }




}