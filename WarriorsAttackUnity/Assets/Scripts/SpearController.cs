using UnityEngine;

public class SpearController : MonoBehaviour
{
    [Header("Configuraci�n")]
    public float speed = 15f;     // Velocidad a la que vuela
    public float lifeTime = 3f;   // Segundos antes de desaparecer si no choca
    public int damage = 1;        // Daño que hace la lanza

    private Rigidbody2D rb;
    public AnimationClip animacionLanza;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = transform.right * speed;

        if (animacionLanza != null)
        {
            Destroy(gameObject, animacionLanza.length);
        }
        else
        {
            Destroy(gameObject, lifeTime);
        }
    }
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Ignoramos al Player para no matarnos a nosotros mismos
        if (hitInfo.CompareTag("Player")) return;


        // Buscamos si lo que hemos tocado tiene el script del Enemigo
        EnemyController enemy = hitInfo.GetComponent<EnemyController>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage); 
        }

        // Destruimos la lanza al chocar contra cualquier cosa
        Destroy(gameObject);

    }
}