using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float slopeRotationSpeed = 20f;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround;
    public float raycastDistance = 1.2f;

    [Header("Recolección y Score")]
    public int monedasParaCurar = 20;
    public int monedasActuales = 0;
    public int monedasTotalesScore = 0;
    public int kills = 0;

    [Header("Interfaz de Usuario")]
    public UIManager uiManager;

    [Header("Límites")]
    public float alturaMuerte = -10f; // Si baja de aquí, muere

    [Header("Audio")]
    public AudioClip sound_Jump;
    public AudioClip sound_Attack;
    public AudioClip sound_Hurt;
    public AudioClip sound_Coin;
    public AudioClip sound_ExtraLife;

    [Header("Visuales")]
    public Transform graficosTransform; // Para rotar el dibujo al subir cuestas

    [Header("Combate")]
    public Transform firePoint;     // Desde donde sale la lanza
    public GameObject spearPrefab;  // El objeto de la lanza
    public float fireRate = 0.5f;   // Tiempo entre disparos

    [Header("Salud")]
    public int maxHealth = 5;
    public int currentHealth;
    public float knockbackForce = 5f; // Fuerza de empuje al recibir daño
    public float knockbackTime = 0.2f;

    // Variables privadas
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;

    private float moveInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isKnockedBack = false;

    // Variables para mejorar el salto (Coyote Time)
    private float hangTime = 0.15f;
    private float hangCounter;
    private float nextFireTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Si se nos olvidó asignar los gráficos, usamos el del animador
        if (graficosTransform == null && anim != null) graficosTransform = anim.transform;

        currentHealth = maxHealth;
        ActualizarUI();
    }

    void Update()
    {
        if (isDead) return;

        // Comprobar si ha caído al vacío
        if (transform.position.y < alturaMuerte)
        {
            Die();
            return;
        }

        if (currentHealth <= 0) return;

        // Leer teclas (A/D o Flechas)
        moveInput = Input.GetAxisRaw("Horizontal");

        // Lógica del "Coyote Time" (permitir saltar un poco después de caer)
        if (isGrounded) hangCounter = hangTime;
        else hangCounter -= Time.deltaTime;

        // Saltar
        if (Input.GetButtonDown("Jump") && hangCounter > 0)
        {
            Saltar();
        }

        // Atacar
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetButtonDown("Fire1")) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        // Control de Animaciones
        anim.SetBool("IsGrounded", isGrounded);
        if (!isKnockedBack)
        {
            anim.SetFloat("Speed", Mathf.Abs(moveInput * moveSpeed));
        }

        AjustarRotacionVisual();
    }

    void FixedUpdate()
    {
        if (isDead || currentHealth <= 0 || isKnockedBack) return;

        CheckGround();

        // Mover al personaje usando físicas
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Girar el personaje si cambia de dirección
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    // --- ACCIONES ---

    void Saltar()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        hangCounter = 0; // Gastamos el salto
        isGrounded = false;
        anim.SetBool("IsGrounded", false);
        PlaySound(sound_Jump);
    }

    void Shoot()
    {
        anim.SetTrigger("Attack");
        PlaySound(sound_Attack);

        // Creamos la lanza y la orientamos según hacia donde miremos
        if (spearPrefab != null && firePoint != null)
        {
            Quaternion rotacion = transform.localScale.x < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            Instantiate(spearPrefab, firePoint.position, rotacion);
        }
    }

    // --- FUNCIONES PÚBLICAS (Llamadas desde fuera) ---

    public void RecogerMoneda(int cantidad)
    {
        if (isDead) return;

        monedasActuales += cantidad;
        monedasTotalesScore += cantidad;
        PlaySound(sound_Coin);

        // Si llegamos al tope, curamos una vida
        if (monedasActuales >= monedasParaCurar)
        {
            Curar(1);
            monedasActuales -= monedasParaCurar;
        }

        ActualizarUI();
    }

    public void RegistrarKill()
    {
        kills++;
    }

    public void Curar(int cantidad)
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += cantidad;
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            ActualizarUI();
            anim.ResetTrigger("Hurt");
            PlaySound(sound_ExtraLife);
        }
    }

    public void TakeDamage(int damageAmount, Transform enemy)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        ActualizarUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Efecto de daño y empuje hacia atrás
            anim.SetTrigger("Hurt");
            PlaySound(sound_Hurt);

            if (enemy != null)
            {
                AplicarKnockback(enemy);
            }
        }
    }

    public int CalcularScoreFinal(bool isVictory)
    {
        int finalScore = 0;
        finalScore += isVictory ? 100 : -50;
        finalScore += (kills * 10);
        finalScore += monedasTotalesScore;
        return Mathf.Max(0, finalScore); // Que no sea negativo
    }

    // --- FUNCIONES INTERNAS ---

    void CheckGround()
    {
        // Comprobamos si los pies tocan el suelo (Círculo + Rayo)
        bool touching = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        if (!touching && rb.linearVelocity.y <= 0.1f)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, raycastDistance, whatIsGround);
            if (hit.collider != null) touching = true;
        }
        isGrounded = touching;
    }

    void AjustarRotacionVisual()
    {
        if (graficosTransform == null) return;
        Quaternion targetRotation = Quaternion.identity;

        // Si estamos en el suelo, rotamos el personaje para que se adapte a la pendiente
        if (hangCounter > 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, raycastDistance + 1f, whatIsGround);
            if (hit.collider != null)
            {
                Vector2 groundNormal = hit.normal;
                float angle = Vector2.SignedAngle(Vector2.up, groundNormal);
                if (Mathf.Abs(angle) < 50f)
                {
                    targetRotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
        graficosTransform.rotation = Quaternion.Lerp(graficosTransform.rotation, targetRotation, Time.deltaTime * slopeRotationSpeed);
    }

    void Flip()
    {
        // Invierte la escala en X para mirar al otro lado
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    void AplicarKnockback(Transform enemy)
    {
        // Empuja al jugador en dirección contraria al enemigo
        Vector2 direction = (transform.position - enemy.position).normalized;
        Vector2 knockbackDir = new Vector2(direction.x, 0.5f).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        // Desactiva el control un momento mientras nos empujan
        isKnockedBack = true;
        yield return new WaitForSeconds(knockbackTime);
        isKnockedBack = false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;

        anim.SetBool("IsDead", true);
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll; // Congelar posición
        gameObject.layer = LayerMask.NameToLayer("Dead"); // Cambiar capa para que no nos peguen más

        if (uiManager != null) uiManager.ActualizarSalud(0);

        // Desactivar seguimiento de cámara
        if (Camera.main != null)
        {
            MonoBehaviour camScript = Camera.main.GetComponent("CameraFollow") as MonoBehaviour;
            if (camScript != null) camScript.enabled = false;
        }

        // Guardar derrota en la nube
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.ActualizarEstadistica("loss", 1);
            int score = CalcularScoreFinal(false);
            FirebaseManager.Instance.ActualizarScore(score);
        }

        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(2f);
        if (uiManager != null)
        {
            int score = CalcularScoreFinal(false);
            uiManager.MostrarPantallaFinal(false, score);
        }
    }

    void ActualizarUI()
    {
        if (uiManager != null)
        {
            uiManager.ActualizarSalud(currentHealth);
            uiManager.ActualizarMonedas(monedasActuales);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    // Muerte instantánea (pinchos, lava)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Deadly") && !isDead) Die();
    }
}