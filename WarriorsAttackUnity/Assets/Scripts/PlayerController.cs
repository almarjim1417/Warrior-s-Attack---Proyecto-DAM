using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("Detecci�n de Suelo")]
    public Transform groundCheck; // El objeto invisible de los pies
    public float groundCheckRadius = 0.2f;
    public LayerMask whatIsGround; // Qu� capa es suelo

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() // Inputs (Teclas)
    {
        // Leer izquierda/derecha
        moveInput = Input.GetAxisRaw("Horizontal");

        // Leer Salto (Espacio)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        // Comprobar si tocamos suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

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
        scaler.x *= -1;
        transform.localScale = scaler;
    }

}