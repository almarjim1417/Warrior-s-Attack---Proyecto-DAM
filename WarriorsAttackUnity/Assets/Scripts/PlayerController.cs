using UnityEngine;
using System.Collections; // Necesario para las Corrutinas (IEnumerator)

public class PlayerController : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("Detección de Suelo")]
    public Transform groundCheck; // El objeto invisible de los pies
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround; // Qué capa es suelo

    [Header("Combate")]
    public Transform firePoint;     // Desde dónde sale la lanza
    public GameObject spearPrefab;  // El prefab de la lanza
    public float fireRate = 0.5f;   // Tiempo de espera entre disparos
    private float nextFireTime = 0f;

    // Variables nuevas para el empujón
    public float knockbackForce = 5f; // Fuerza con la que salimos despedidos
    public float knockbackTime = 0.2f; // Tiempo que perdemos el control
    private bool isKnockedBack = false; // Controlamos si nos están empujando

    [Header("Salud")]
    public int maxHealth = 5;
    public int currentHealth;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isFacingRight = true;

    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth; // Iniciamos la vida al máximo
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Si estamos recibiendo un ataque, estamos "quietos"
        if (isGrounded && !isKnockedBack && Mathf.Abs(moveInput) > 0.1f)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }
        else
        {
            anim.SetFloat("Speed", 0f);
        }

        // Lógica de disparo con F o Click Izqdo
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetButtonDown("Fire1")) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void FixedUpdate()
    {
        // Si nos están empujando, no dejamos que el jugador se mueva
        if (isKnockedBack) return;

        // Comprobar si tocamos suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        anim.SetBool("IsGrounded", isGrounded);

        // Mover el cuerpo
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Girar el personaje (Flip)
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;

        // Averiguamos cuánto mide Lincoln ahora mismo
        float currentSize = Mathf.Abs(scaler.x);

        // Aplicamos el signo según la dirección
        if (isFacingRight)
        {
            scaler.x = currentSize;
        }
        else
        {
            scaler.x = -currentSize;
        }

        transform.localScale = scaler;
    }


    public void TakeDamage(int damageAmount, Transform enemy)
    {
        // Restamos vida
        currentHealth -= damageAmount;
        Debug.Log("¡Auch! Vida restante: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");
        }

        if (enemy != null)
        {
            Vector2 direction = (transform.position - enemy.position).normalized;
            Vector2 knockbackDir = new Vector2(direction.x, 0.5f).normalized;

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

            StartCoroutine(KnockbackRoutine());
        }
    }

    public void Die()
    {
        anim.ResetTrigger("Hurt");
        anim.SetBool("IsDead", true);

        this.enabled = false;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        gameObject.tag = "Untagged";

        BoxCollider2D collider = GetComponent<BoxCollider2D>();

        collider.size = new Vector2(0.1f, 0.1f);
        collider.offset = new Vector2(0f, 0.1f);
    }
    void Shoot()
    {
        anim.SetTrigger("Attack");

        // Por defecto ataca hacia la dcha
        Quaternion rotacionLanza = Quaternion.identity;

        // Si la escala del player es <0, rotamos la lanza
        if (transform.localScale.x < 0)
        {
            rotacionLanza = Quaternion.Euler(0, 180, 0);
        }

        Instantiate(spearPrefab, firePoint.position, rotacionLanza);
    }

    // Rutina para esperar un momento mientras nos empujan
    IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(knockbackTime);
        isKnockedBack = false;
    }
}