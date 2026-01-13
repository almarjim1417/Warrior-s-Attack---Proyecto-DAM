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
    public Transform graficosTransform; // Para rotar el player en la rampa

    [Header("Combate")]
    public Transform firePoint;
    public GameObject spearPrefab; 
    public float fireRate = 0.5f;   // Tiempo entre disparos

    [Header("Salud")]
    public int maxHealth = 5;
    public int currentHealth;
    public float knockbackForce = 5f; // Retroceso al ser golpeado
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

    // "Salto"
    private float hangTime = 0.15f;
    private float hangCounter;
    private float nextFireTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Comprobaciómn
        if (graficosTransform == null && anim != null) graficosTransform = anim.transform;

        currentHealth = maxHealth;
        ActualizarUI();
    }

    void Update()
    {
        if (!isDead)
        {
            // Comprobar si ha caído al vacío
            if (transform.position.y < alturaMuerte)
            {
                Die();
            }
            else if (currentHealth > 0)
            {
                moveInput = Input.GetAxisRaw("Horizontal");

                // Lógica del "Coyote Time": Si estamos en el suelo, el margen está a tope (hangTime), y si no, lo vamos gastando poco a poco con el Time.deltaTime
                if (isGrounded) hangCounter = hangTime;
                else hangCounter -= Time.deltaTime;

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
        }
    }

    void FixedUpdate()
    {
        if (!isDead && currentHealth > 0 && !isKnockedBack)
        {
            CheckGround();
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            // Girar el personaje si cambia de dirección
            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();
        }
    }

    // ACCIONES 

    void Saltar()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        hangCounter = 0; // Reiniciamos el contador
        isGrounded = false;
        anim.SetBool("IsGrounded", false);
        PlaySound(sound_Jump);
    }

    void Shoot()
    {
        anim.SetTrigger("Attack");
        PlaySound(sound_Attack);

        if (spearPrefab != null && firePoint != null)
        {
            Quaternion rotacion = transform.localScale.x < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            Instantiate(spearPrefab, firePoint.position, rotacion);
        }
    }

    // FUNCIONES PÚBLICAS

    public void RecogerMoneda(int cantidad)
    {
        if (!isDead)
        {
            monedasActuales += cantidad;
            monedasTotalesScore += cantidad;
            PlaySound(sound_Coin);

            // Si llegamos a 20, regeneramos un corazón
            if (monedasActuales >= monedasParaCurar)
            {
                Curar(1);
                monedasActuales -= monedasParaCurar;
            }

            ActualizarUI();
        }
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
            // Si nos curamos mientras somos golpeados, no cuenta el golpe
            anim.ResetTrigger("Hurt");
            PlaySound(sound_ExtraLife);
        }
    }

    public void TakeDamage(int damageAmount, Transform enemy)
    {
        if (!isDead)
        {
            currentHealth -= damageAmount;
            ActualizarUI();

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Animación y knockback
                anim.SetTrigger("Hurt");
                PlaySound(sound_Hurt);

                if (enemy != null)
                {
                    AplicarKnockback(enemy);
                }
            }
        }
    }

    public int CalcularScoreFinal(bool isVictory)
    {
        int finalScore = 0;
        finalScore += isVictory ? 100 : -50;
        finalScore += (kills * 10);
        finalScore += monedasTotalesScore;
        return Mathf.Max(0, finalScore); // Si es negativo, se queda a 0
    }

    // FUNCIONES PRIVADAS

    void CheckGround()
    {
        // Comprobamos si los pies tocan el suelo con un circulo en los pies 
        bool touching = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        // Si existe devuelve true, si no, false


        if (!touching && rb.linearVelocity.y <= 0.1f)
        {
            // En caso de no existir, comprobamos con un Raycast, un rayo que sale de los pies del jugador hacia abajo para comprobar si toca suelo, útil por ejemplo en una rampa
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, raycastDistance, whatIsGround);
            if (hit.collider != null) touching = true;
        }
        isGrounded = touching;
    }

    void AjustarRotacionVisual()
    {
        if (graficosTransform != null)
        {
            Quaternion targetRotation = Quaternion.identity;

            // Si estamos en el suelo, rotamos el personaje para que se adapte a la pendiente
            if (hangCounter > 0)
            {
                // Lanzamos un raycast desde el jugador hacaia bajo
                RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, raycastDistance + 1f, whatIsGround);
                if (hit.collider != null)
                {
                    // Calculamos la normal(la perpendicular) del objeto golpeado por el raycast (el suelo/rampa)
                    Vector2 groundNormal = hit.normal;
                    // Con SingledAngle calculamos la diferencia de los grados entre la normal del suelo/rampa y 0º, es decir arriba
                    float angle = Vector2.SignedAngle(Vector2.up, groundNormal);
                    // Si la rampa es muy inclinada la ignora para por ejemplo no trepar paredes
                    if (Mathf.Abs(angle) < 50f)
                    {
                        targetRotation = Quaternion.Euler(0, 0, angle);
                    }
                }
            }
            // Ajusta la inclinación del player suavemente (con Lerp) a la rotación calculada
            graficosTransform.rotation = Quaternion.Lerp(graficosTransform.rotation, targetRotation, Time.deltaTime * slopeRotationSpeed);
        }
    }

    void Flip()
    {
        // Cambiamos el valor de isFacingRight al contrario
        isFacingRight = !isFacingRight;
        // Copiamos el tamaño del personaje y multiplicamos el vector x por -1.
        Vector3 reverseScale = transform.localScale;
        reverseScale.x *= -1;
        transform.localScale = reverseScale;
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
        if (!isDead)
        {
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
                // Obtenemos el s
                MonoBehaviour camScript = (MonoBehaviour)Camera.main.GetComponent("CameraFollow");
                if (camScript != null) camScript.enabled = false;
            }

            // Calculamos el Score
            int finalScore = CalcularScoreFinal(false);

            // Actualizamos la bd con la derrota y añadimos el nuevo score
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.ActualizarEstadistica("loss", 1);
                FirebaseManager.Instance.ActualizarScore(finalScore);
            }

            StartCoroutine(GameOverSequence(finalScore));
        }
    }

    IEnumerator GameOverSequence(int scoreCongelado)
    {
        yield return new WaitForSeconds(2f);
        if (uiManager != null)
        {
            uiManager.MostrarPantallaFinal(false, scoreCongelado);
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

    // Muerte instantánea
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Deadly") && !isDead) Die();
    }
}