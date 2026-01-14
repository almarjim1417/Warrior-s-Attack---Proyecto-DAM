using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    public bool isDead = false;
    public bool isActive = false;
    public string bossName = "The Overlord";

    [Header("References")]
    public Transform player;
    public UIManager uiManager;
    public AudioSource audioSource;
    public Transform attackPoint;
    public Transform firePoint;
    public GameObject rockPrefab;

    [Header("Audio Clips")]
    public AudioClip sound_BossAttack;
    public AudioClip sound_BossDead;
    public AudioClip sound_BossHurt;

    [Header("Movement & Combat Stats")]
    public float moveSpeed = 3f;
    public float minXPosition = -10f;
    public float maxXPosition = 10f;
    public float meleeDetectionRange = 8f;
    public float strikingDistance = 2.5f;
    public float rangedRange = 15f;
    public float attackCooldown = 2f;
    public int meleeDamage = 20;
    public float attackRadius = 1.5f;
    public LayerMask playerLayer;
    public float safeZoneHeight = 3.0f;

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

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void Despertar()
    {
        isActive = true;
    }

    void Update()
    {
        if (!isActive || isDead || player == null) return;
        if (!player.CompareTag("Player")) { StopMoving(); return; }
        if (IsPlayingAttackAnimation() || isHurt) { rb.linearVelocity = Vector2.zero; return; }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float diffY = transform.position.y - player.position.y;

        // Logic checks
        bool inTrench = diffY > safeZoneHeight;
        bool isMeleeReachable = !inTrench && (diffY > -3.0f);
        bool inRangedArea = !inTrench && (distanceToPlayer <= rangedRange);
        bool isStoppingToHit = distanceToPlayer <= strikingDistance;

        // Animator updates
        anim.SetBool("InMeleeRange", isStoppingToHit && isMeleeReachable);
        bool isMoving = rb.linearVelocity.magnitude > 0.1f;
        anim.SetBool("InRangedArea", inRangedArea && distanceToPlayer > meleeDetectionRange && !isMoving);

        LookAtPlayer();

        if (inTrench)
        {
            StopMoving();
            return;
        }

        // Combat State Machine
        if (distanceToPlayer <= meleeDetectionRange && isMeleeReachable)
        {
            if (distanceToPlayer <= strikingDistance)
            {
                StopMoving();
                if (Time.time >= nextAttackTime) AttackMelee();
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
        else if (distanceToPlayer <= rangedRange)
        {
            StopMoving();
            if (Time.time >= nextAttackTime) AttackRanged();
        }
        else
        {
            StopMoving();
        }
    }

    // --- Movement Logic ---

    void MoveTowardsPlayer()
    {
        float direction = (player.position.x - transform.position.x) > 0 ? 1 : -1;

        // Bounds check
        if ((direction > 0 && transform.position.x >= maxXPosition) ||
            (direction < 0 && transform.position.x <= minXPosition))
        {
            StopMoving();
            return;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        anim.SetFloat("Speed", Mathf.Abs(moveSpeed));
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetFloat("Speed", 0f);
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

    // --- Combat Logic ---

    void AttackMelee()
    {
        StopMoving();
        anim.SetTrigger("AttackMelee");
        PlaySound(sound_BossAttack);
        nextAttackTime = Time.time + attackCooldown;
    }

    void AttackRanged()
    {
        StopMoving();
        anim.SetTrigger("AttackRanged");
        PlaySound(sound_BossAttack);
        nextAttackTime = Time.time + attackCooldown + 1.5f;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (!isActive) Despertar();

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

    // --- Animation Events ---

    public void GolpeMelee()
    {
        if (attackPoint == null) return;
        Collider2D[] hit = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider2D p in hit)
            p.GetComponent<PlayerController>()?.TakeDamage(meleeDamage, transform);
    }

    public void LanzarRoca()
    {
        if (rockPrefab != null && firePoint != null)
        {
            GameObject r = Instantiate(rockPrefab, firePoint.position, Quaternion.identity);
            r.GetComponent<BossProjectile>()?.Lanzar(player.position - firePoint.position);
        }
    }

    // --- Death & Helpers ---

    void Die()
    {
        isDead = true;
        anim.SetBool("IsDead", true);
        rb.linearVelocity = Vector2.zero;

        PlaySound(sound_BossDead);

        if (Camera.main != null) Camera.main.GetComponent<AudioSource>()?.Stop();
        if (uiManager != null) uiManager.OcultarBossUI();

        int finalScore = 0;
        if (player != null)
        {
            PlayerController p = player.GetComponent<PlayerController>();
            if (p != null)
            {
                p.RegistrarKill();
                finalScore = p.CalcularScoreFinal(true);
            }
        }

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.ActualizarEstadistica("win", 1);
            FirebaseManager.Instance.ActualizarScore(finalScore);
        }

        GetComponent<Collider2D>().enabled = false;
        rb.bodyType = RigidbodyType2D.Static;
        StartCoroutine(VictorySequence(finalScore));
    }

    IEnumerator VictorySequence(int score)
    {
        yield return new WaitForSeconds(3f);
        if (uiManager != null) uiManager.MostrarPantallaFinal(true, score);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    bool IsPlayingAttackAnimation()
    {
        AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);
        return s.IsName("Boss_Punch") || s.IsName("Boss_Throw");
    }
}