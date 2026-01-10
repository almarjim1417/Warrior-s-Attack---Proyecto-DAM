using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Configuración Básica")]
    public int maxHealth = 100;
    private int currentHealth;
    public bool isDead = false;
    public bool isActive = false;
    public string bossName = "The Overlord";

    [Header("Referencias")]
    public Transform player; // A quién perseguimos
    public UIManager uiManager; // Para la barra de vida
    public AudioSource audioSource;
    public Transform attackPoint; // Desde dónde pegamos el puñetazo
    public Transform firePoint;   // Desde dónde lanzamos la roca
    public GameObject rockPrefab; // La roca que lanzamos

    [Header("Sonidos")]
    public AudioClip sound_BossAttack;
    public AudioClip sound_BossDead;
    public AudioClip sound_BossHurt;

    [Header("Estadísticas de Combate")]
    public float moveSpeed = 3f;
    public float meleeDetectionRange = 8f; // Si estás cerca te persigue para pegar
    public float strikingDistance = 2.5f;  // Distancia para pararse y pegar
    public float rangedRange = 15f;        // Si estás lejos te tira piedras
    public float attackCooldown = 2f;      // Tiempo entre ataques
    public int meleeDamage = 20;
    public float attackRadius = 1.5f;      // Tamaño del golpe
    public LayerMask playerLayer;
    public float safeZoneHeight = 3.0f;    // Altura donde el boss no llega (trinchera)

    // Límites para que el boss no se salga del mapa
    public float minXPosition = -10f;
    public float maxXPosition = 10f;

    private float nextAttackTime = 0f;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isFacingRight = false;
    private bool isHurt = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentHealth = maxHealth;

        // Buscamos al jugador si no lo hemos arrastrado
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void Despertar()
    {
        // Función llamada cuando el jugador entra en la zona del Boss
        isActive = true;
    }

    void Update()
    {
        // Si no está activo, está muerto o no hay jugador, no hacemos nada
        if (!isActive || isDead || player == null) return;

        // Si estamos atacando o recibiendo daño, nos quedamos quietos
        if (IsPlayingAttackAnimation() || isHurt) { rb.linearVelocity = Vector2.zero; return; }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float diffY = transform.position.y - player.position.y;

        // Lógica de Estado:
        bool inTrench = diffY > safeZoneHeight; // ¿Está el jugador escondido abajo?
        bool isMeleeReachable = !inTrench && (diffY > -3.0f); // ¿Podemos alcanzarle cuerpo a cuerpo?

        // Mirar siempre al jugador
        LookAtPlayer();

        // Si el jugador está escondido en la trinchera, el boss espera
        if (inTrench)
        {
            StopMoving();
            return;
        }

        // MÁQUINA DE ESTADOS DE COMBATE
        // 1. Si está cerca -> Ataque cuerpo a cuerpo
        if (distanceToPlayer <= meleeDetectionRange && isMeleeReachable)
        {
            if (distanceToPlayer <= strikingDistance)
            {
                StopMoving(); // Parar para golpear
                if (Time.time >= nextAttackTime) AttackMelee();
            }
            else
            {
                MoveTowardsPlayer(); // Correr hacia él
            }
        }
        // 2. Si está lejos pero a la vista -> Ataque a distancia
        else if (distanceToPlayer <= rangedRange)
        {
            StopMoving();
            if (Time.time >= nextAttackTime) AttackRanged();
        }
        // 3. Si está muy lejos -> Esperar
        else
        {
            StopMoving();
        }

        // Actualizar animaciones
        bool isMoving = rb.linearVelocity.magnitude > 0.1f;
        anim.SetFloat("Speed", isMoving ? Mathf.Abs(moveSpeed) : 0f);
    }

    // --- Movimiento ---

    void MoveTowardsPlayer()
    {
        float direction = (player.position.x - transform.position.x) > 0 ? 1 : -1;

        // Comprobamos que no se salga de los límites del mapa
        if ((direction > 0 && transform.position.x >= maxXPosition) ||
            (direction < 0 && transform.position.x <= minXPosition))
        {
            StopMoving();
            return;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void LookAtPlayer()
    {
        if (player.position.x > transform.position.x && !isFacingRight) Flip();
        else if (player.position.x < transform.position.x && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    // --- Ataques ---

    void AttackMelee()
    {
        StopMoving();
        anim.SetTrigger("AttackMelee"); // Dispara la animación
        PlaySound(sound_BossAttack);
        nextAttackTime = Time.time + attackCooldown;
    }

    void AttackRanged()
    {
        StopMoving();
        anim.SetTrigger("AttackRanged"); // Dispara la animación de lanzar
        PlaySound(sound_BossAttack);
        nextAttackTime = Time.time + attackCooldown + 1.5f; // Un poco más lento que el melee
    }

    // --- Eventos de Animación (Animation Events) ---
    // Estas funciones son llamadas DESDE LA ANIMACIÓN justo en el frame del golpe

    public void GolpeMelee()
    {
        // Detectamos si el jugador está en el círculo de ataque
        if (attackPoint == null) return;
        Collider2D[] hit = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider2D p in hit)
            p.GetComponent<PlayerController>()?.TakeDamage(meleeDamage, transform);
    }

    public void LanzarRoca()
    {
        // Creamos la roca y le decimos hacia dónde volar
        if (rockPrefab != null && firePoint != null)
        {
            GameObject r = Instantiate(rockPrefab, firePoint.position, Quaternion.identity);
            r.GetComponent<BossProjectile>()?.Lanzar(player.position - firePoint.position);
        }
    }

    // --- Daño y Muerte ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (!isActive) Despertar(); // Si le pegamos dormido, se despierta

        currentHealth -= damage;
        if (uiManager != null) uiManager.ActualizarVidaBoss(currentHealth);

        PlaySound(sound_BossHurt);
        anim.SetTrigger("Hurt");
        StartCoroutine(HurtRoutine());

        if (currentHealth <= 0) Die();
    }

    IEnumerator HurtRoutine()
    {
        isHurt = true;
        yield return new WaitForSeconds(0.4f);
        isHurt = false;
    }

    void Die()
    {
        isDead = true;
        anim.SetBool("IsDead", true);
        rb.linearVelocity = Vector2.zero;

        PlaySound(sound_BossDead);

        // Paramos música y ocultamos UI
        if (Camera.main != null) Camera.main.GetComponent<AudioSource>()?.Stop();
        if (uiManager != null) uiManager.OcultarBossUI();

        // Calculamos puntuación final
        int finalScore = 0;
        if (player != null)
        {
            PlayerController p = player.GetComponent<PlayerController>();
            if (p != null)
            {
                p.RegistrarKill();
                finalScore = p.CalcularScoreFinal(true); // true = Victoria
            }
        }

        // Guardamos en Firebase
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.ActualizarEstadistica("win", 1);
            FirebaseManager.Instance.ActualizarScore(finalScore);
        }

        // Desactivamos colisiones para que no estorbe
        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Static;

        StartCoroutine(VictorySequence(finalScore));
    }

    IEnumerator VictorySequence(int score)
    {
        // Esperamos 3 segundos para celebrar antes de mostrar el menú
        yield return new WaitForSeconds(3f);
        if (uiManager != null) uiManager.MostrarPantallaFinal(true, score);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    bool IsPlayingAttackAnimation()
    {
        // Comprueba si estamos en mitad de una animación de ataque
        AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);
        return s.IsName("Boss_Punch") || s.IsName("Boss_Throw");
    }
}