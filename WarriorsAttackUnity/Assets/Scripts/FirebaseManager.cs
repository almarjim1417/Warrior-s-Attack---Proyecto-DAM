using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase; // ¡Importante!

public class FirebaseManager : MonoBehaviour
{
    // Esta variable la haremos pública para que otros scripts
    // puedan saber si Firebase está listo
    public static bool isFirebaseReady = false;

    // Start se llama una vez al inicio
    void Start()
    {
        // Hacemos que este objeto no se destruya al cambiar de escena
        // Así la conexión se mantiene siempre activa
        DontDestroyOnLoad(this.gameObject);

        // Inicia el proceso de comprobación e inicialización
        StartCoroutine(InitializeFirebase());
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
            // ¡Éxito! Firebase está listo para usarse.
            Debug.Log("Firebase inicializado correctamente.");
            isFirebaseReady = true; // Marcamos como listo
            
            // Inicializamos la App (esto es solo una referencia)
            Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
        }
        else
        {
            // ¡Error! Algo ha fallado.
            Debug.LogError($"No se pudieron resolver las dependencias de Firebase: {dependencyStatus}");
            isFirebaseReady = false;
        }
    }
}
