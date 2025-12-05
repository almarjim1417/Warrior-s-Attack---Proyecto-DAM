using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public Texture2D defaultCursor;
    public Texture2D textCursor;

    public static bool isFirebaseReady = false;

    // Awake se ejecuta antes que el Start
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            // Si ya existe un FirebaseManager, este se destruye
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        SetDefaultCursor();

        StartCoroutine(InitializeFirebase());
    }

    public void SetDefaultCursor()
    {
        // Ponemos la lanza de cursor
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }

    public void SetTextCursor()
    {
        // Ponemos el cursor de texto
        Vector2 hotspot = new Vector2(textCursor.width / 2, textCursor.height / 2);
        Cursor.SetCursor(textCursor, hotspot, CursorMode.Auto);
    }

    private IEnumerator InitializeFirebase()
    {
        Debug.Log("Comprobando dependencias de Firebase...");

        // Comprueba si todas las dependencias necesarias están listas
        var checkTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        
        // Espera hasta que la tarea de comprobación termine
        yield return new WaitUntil(() => checkTask.IsCompleted);

        // Obtenemos el resultado de la comprobación
        var dependencyStatus = checkTask.Result;

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            Debug.Log("Firebase inicializado correctamente.");
            isFirebaseReady = true;
            
            // Inicializamos la App
            Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
        }
        else
        {
            Debug.LogError($"No se pudieron resolver las dependencias de Firebase: {dependencyStatus}");
            isFirebaseReady = false;
        }
    }
}
