using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase; // ¡Importante!

public class FirebaseManager : MonoBehaviour
{
    public static bool isFirebaseReady = false;

    void Start()
    {
        // Hacemos que este objeto no se destruya al cambiar de escena
        DontDestroyOnLoad(this.gameObject);

        // Inicia el proceso de  inicialización
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
            Debug.Log("Firebase inicializado correctamente.");
            isFirebaseReady = true; // Marcamos como listo
            
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
