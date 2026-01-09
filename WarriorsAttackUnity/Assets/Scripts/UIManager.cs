using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Cosas del Player")]
    public TextMeshProUGUI textoMonedas; // El numerito de las monedas

    [Header("Vida del Player")]
    public Image[] corazones; // Array con las imagenes de los corazones
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    [Header("Barra del Boss")]
    public GameObject panelBoss;
    public Slider sliderBoss;
    public TextMeshProUGUI textoNombreBoss;

    [Header("Pantalla Final")]
    public GameObject panelFinal; // El panel gris que tapa la pantalla al acabar
    public TextMeshProUGUI textoTitulo; // Para poner VICTORIA o GAME OVER
    public TextMeshProUGUI textoScore;

    [Header("Botones Menú")]
    public Button botonMenu;
    public Button botonSalir;

    [Header("Música Final")]
    public AudioClip music_Victory;
    public AudioClip music_Defeat;

    private AudioSource audioSource; // El altavoz para que suene la victoria/derrota

    void Start()
    {
        // Buscamos el componente de audio. Si no está, lo añadimos para que no de error
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Preparamos los botones si los hemos arrastrado en el inspector
        if (botonMenu != null)
        {
            botonMenu.onClick.RemoveAllListeners(); // Limpiamos por si acaso
            botonMenu.onClick.AddListener(IrAlDashboard);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.RemoveAllListeners();
            botonSalir.onClick.AddListener(CerrarJuego);
        }
    }

    // --- HUD DEL JUGADOR ---

    public void ActualizarMonedas(int cantidad)
    {
        // Solo actualizo si el texto está asignado
        if (textoMonedas != null) textoMonedas.text = cantidad.ToString();
    }

    public void ActualizarSalud(int saludActual)
    {
        // Recorro todos los corazones (tengo 3 o 5 en el array)
        for (int i = 0; i < corazones.Length; i++)
        {
            // Si mi salud es mayor que el índice, pongo corazón lleno, si no, vacío
            if (i < saludActual) corazones[i].sprite = corazonLleno;
            else corazones[i].sprite = corazonVacio;
        }
    }

    // --- HUD DEL BOSS ---

    public void ActivarBossUI(int vidaMaxima, string nombre)
    {
        if (panelBoss != null)
        {
            panelBoss.SetActive(true); // Que aparezca la barra
            sliderBoss.maxValue = vidaMaxima; // Configuramos el tope de la barra
            sliderBoss.value = vidaMaxima; // La llenamos a tope al empezar

            if (textoNombreBoss != null) textoNombreBoss.text = nombre;
        }
    }

    public void ActualizarVidaBoss(int vidaActual)
    {
        // Vamos bajando la barra según le pegamos
        if (sliderBoss != null) sliderBoss.value = vidaActual;
    }

    public void OcultarBossUI()
    {
        // Cuando muere el boss, escondemos la barra
        if (panelBoss != null) panelBoss.SetActive(false);
    }

    // --- PANTALLA FINAL (GANAR O PERDER) ---

    public void MostrarPantallaFinal(bool esVictoria, int scorePartida)
    {
        if (panelFinal != null)
        {
            panelFinal.SetActive(true);

            // 1. Parar la música del nivel
            // Buscamos la cámara porque ahí es donde suele estar la música de fondo
            if (Camera.main != null)
            {
                AudioSource musicaFondo = Camera.main.GetComponent<AudioSource>();
                if (musicaFondo != null) musicaFondo.Stop();
            }

            // 2. Poner la música de final
            if (audioSource != null)
            {
                audioSource.loop = false; // Que suene solo una vez

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

            // 3. Cambiar el texto y el color según qué haya pasado
            if (textoTitulo != null)
            {
                if (esVictoria)
                {
                    textoTitulo.text = "¡VICTORIA!";
                    textoTitulo.color = Color.green; // Verde si ganas
                }
                else
                {
                    textoTitulo.text = "GAME OVER";
                    textoTitulo.color = Color.red; // Rojo si pierdes
                }
            }

            // 4. Mostrar puntos
            if (textoScore != null)
            {
                textoScore.text = "Puntuación: " + scorePartida.ToString();
            }

            // 5. Parar el tiempo del juego para que nada se mueva
            Time.timeScale = 0f;
        }
    }

    // --- CAMBIO DE ESCENAS ---

    public void IrAlDashboard()
    {
        Time.timeScale = 1f; // Importante: Volver a activar el tiempo o el menú se quedará congelado
        SceneManager.LoadScene("DashboardScene");
    }

    public void CerrarJuego()
    {
        Debug.Log("Saliendo...");
        Application.Quit();

        // Esto es un truco para que el botón de salir funcione también dentro del editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}