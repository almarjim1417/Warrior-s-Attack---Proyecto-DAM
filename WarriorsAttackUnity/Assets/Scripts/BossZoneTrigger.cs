using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Environment")]
    public GameObject muroBoss;

    [Header("References")]
    public UIManager uiManager;
    public BossController bossController;

    [Header("Audio Settings")]
    public AudioSource levelAudioSource;
    public AudioClip bossMusic;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // "Guard Clause": Si ya se activó o no es el jugador, salimos.
        if (hasTriggered || !collision.CompareTag("Player")) return;

        hasTriggered = true;
        StartBossEvent();
    }

    void StartBossEvent()
    {
        // 1. Bloquear la zona
        if (muroBoss != null)
            muroBoss.SetActive(true);

        // 2. Configurar Boss y UI
        if (bossController != null)
        {
            bossController.Despertar();

            if (uiManager != null)
                uiManager.ActivarBossUI(bossController.maxHealth, "The Overlord");
        }

        // 3. Cambio de Atmósfera (Música)
        if (levelAudioSource != null && bossMusic != null)
        {
            levelAudioSource.Stop();
            levelAudioSource.clip = bossMusic;
            levelAudioSource.loop = true;
            levelAudioSource.Play();
        }
    }
}