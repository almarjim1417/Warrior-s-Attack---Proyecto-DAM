using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject muroBoss; // El muro invisible que cierra la salida
    public UIManager uiManager;

    public BossController bossScript;

    [Header("Audio")]
    public AudioSource audioSourceNivel; // El altavoz de la cámara
    public AudioClip musicaBoss; // La canción de pelea

    private bool eventoActivado = false; // Para que no se repita si entras y sales

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo activamos si entra el Jugador y si no ha pasado ya antes
        if (eventoActivado || !collision.CompareTag("Player")) return;

        eventoActivado = true;
        iniciarEventoBoss();
    }

    void iniciarEventoBoss()
    {
        // Activamos el muro para bloquear la salida
        if (muroBoss != null) muroBoss.SetActive(true);

        // Despertamos al Boss y mostramos su barra de vida en la pantalla
        if (bossScript != null)
        {
            bossScript.Despertar();

            if (uiManager != null)
                uiManager.ActivarBossUI(bossScript.maxHealth, "The Overlord");
        }

        // Cambiamos la música de fondo por la música de tensión
        if (audioSourceNivel != null && musicaBoss != null)
        {
            audioSourceNivel.Stop();       // Paramos la música normal
            audioSourceNivel.clip = musicaBoss;
            audioSourceNivel.loop = true;  // Que se repita en bucle
            audioSourceNivel.Play();       // Le damos al play
        }
    }
}