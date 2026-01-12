using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject muroBoss; // El muro invisible
    public UIManager uiManager;

    public BossController bossScript;

    [Header("Audio")]
    public AudioSource audioSourceNivel; // El altavoz de la cámara
    public AudioClip musicaBoss;

    private bool eventoActivado = false; // Control del trigger

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo se activa si pasa el jugador y es su primera vez
        if (!eventoActivado && collision.CompareTag("Player"))
        {
            eventoActivado = true;
            iniciarEventoBoss();
        }
    }

    void iniciarEventoBoss()
    {
        // Activamos el muro para bloquear la salida
        if (muroBoss != null) muroBoss.SetActive(true);

        // Despertamos al Boss y mostramos su barra de vida en el UI
        if (bossScript != null)
        {
            bossScript.Despertar();

            if (uiManager != null)
                uiManager.ActivarBossUI(bossScript.maxHealth, "The Overlord");
        }

        // Cambiamos la música de fondo por la música de tensión
        if (audioSourceNivel != null && musicaBoss != null)
        {
            audioSourceNivel.Stop(); 
            audioSourceNivel.clip = musicaBoss;
            audioSourceNivel.loop = true; 
            audioSourceNivel.Play(); 
        }
    }
}