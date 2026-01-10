using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Interfaz Jugador")]
    public TextMeshProUGUI textoMonedas;

    [Header("Vida")]
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    [Header("Jefe Final")]
    public GameObject panelBoss;
    public Slider sliderBoss;
    public TextMeshProUGUI textoNombreBoss;

    [Header("Pantalla Final")]
    public GameObject panelFinal;
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoScore;

    [Header("Botones")]
    public Button botonMenu;
    public Button botonSalir;

    [Header("Audio Final")]
    public AudioClip music_Victory;
    public AudioClip music_Defeat;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Configurar botones
        if (botonMenu != null)
        {
            botonMenu.onClick.RemoveAllListeners();
            botonMenu.onClick.AddListener(IrAlDashboard);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(CerrarJuego);
        }
    }

    // --- HUD JUGADOR ---

    public void ActualizarMonedas(int cantidad)
    {
        if (textoMonedas != null) textoMonedas.text = cantidad.ToString();
    }

    public void ActualizarSalud(int saludActual)
    {
        // Recorremos los corazones para pintarlos llenos o vacíos según la vida actual
        for (int i = 0; i < corazones.Length; i++)
        {
            if (i < saludActual) corazones[i].sprite = corazonLleno;
            else corazones[i].sprite = corazonVacio;
        }
    }

    // --- HUD JEFE ---

    public void ActivarBossUI(int vidaMaxima, string nombre)
    {
        if (panelBoss != null)
        {
            panelBoss.SetActive(true); // Mostramos la barra
            sliderBoss.maxValue = vidaMaxima;
            sliderBoss.value = vidaMaxima;

            if (textoNombreBoss != null) textoNombreBoss.text = nombre;
        }
    }

    public void ActualizarVidaBoss(int vidaActual)
    {
        if (sliderBoss != null) sliderBoss.value = vidaActual;
    }

    public void OcultarBossUI()
    {
        if (panelBoss != null) panelBoss.SetActive(false);
    }

    // --- PANTALLA FINAL ---

    public void MostrarPantallaFinal(bool esVictoria, int scorePartida)
    {
        if (panelFinal != null)
        {
            panelFinal.SetActive(true);

            // 1. Parar la música del nivel (normalmente en la cámara)
            if (Camera.main != null)
            {
                AudioSource musicaFondo = Camera.main.GetComponent<AudioSource>();
                if (musicaFondo != null) musicaFondo.Stop();
            }

            // 2. Poner música de victoria o derrota
            if (audioSource != null)
            {
                audioSource.loop = false;
                if (esVictoria && music_Victory != null)
                {
                    audioSource.clip = music_Victory;
                    audioSource.Play();
                }
                else if (!esVictoria && music_Defeat != null)
                {
                    audioSource.clip = music_Defeat;
                    audioSource.Play();
                }
            }

            // 3. Configurar textos y colores
            if (textoTitulo != null)
            {
                if (esVictoria)
                {
                    textoTitulo.text = "¡VICTORIA!";
                    textoTitulo.color = Color.green;
                }
                else
                {
                    textoTitulo.text = "GAME OVER";
                    textoTitulo.color = Color.red;
                }
            }

            if (textoScore != null)
            {
                textoScore.text = "Puntuación: " + scorePartida.ToString();
            }

            // 4. Parar el tiempo del juego
            Time.timeScale = 0f;
        }
    }

    // --- NAVEGACIÓN ---

    public void IrAlDashboard()
    {
        Time.timeScale = 1f; // Reactivamos el tiempo antes de cambiar de escena
        SceneManager.LoadScene("DashboardScene");
    }

    public void CerrarJuego()
    {
        Application.Quit();

        // Para que en el editor también se detenga el juego
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}