using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Firebase
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase; // Tareas asíncronas

public class AuthManager : MonoBehaviour
{

    // --- Referencias a Firebase ---
    private FirebaseAuth auth;          //Instancia de Autenticación
    private FirebaseFirestore db;     //Instancia de la Base de Datos

    [Header("--- Paneles de UI ---")]
    public GameObject welcomePanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("--- Botones de Navegación ---")]
    public Button goToLoginButton;
    public Button goToRegisterButton;
    public Button backFromLoginButton;
    public Button backFromRegisterButton;

    [Header("--- Campos de Login ---")]
    public TMP_InputField usernameLoginInput;
    public TMP_InputField passwordLoginInput;
    public Button loginButton;

    [Header("--- Campos de Registro ---")]
    public TMP_InputField usernameRegisterInput;
    public TMP_InputField emailRegisterInput;
    public TMP_InputField passwordRegisterInput;
    public Button registerButton;

    [Header("--- Otros ---")]
    public TMP_Text errorText;


    void Start()
    {
        // Configurar el estado inicial: solo se ve el panel de bienvenida
        ShowWelcomePanel();

        // Asignar funciones a los botones de navegación
        goToLoginButton.onClick.AddListener(ShowLoginPanel);
        goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        backFromLoginButton.onClick.AddListener(ShowWelcomePanel);
        backFromRegisterButton.onClick.AddListener(ShowWelcomePanel);

        // Asignar funciones a los botones de acción (Login y Registro)
        loginButton.onClick.AddListener(HandleLogin);
        registerButton.onClick.AddListener(HandleRegister);
        
        //Inizializar Firebase
        StartCoroutine(InitializeFirebaseAndCheck());
    }

    // Corutina para esperar a FirebaseManager
    // Usamos corutina por eficiencia ya que esta función permite pausarse cuando lo deseemos y
    // como lo vamos a usar para esperar a que el otro script realice la conexión con firebase, es lo más eficiente.
    private IEnumerator InitializeFirebaseAndCheck()
    {
        // Espera hasta que la variable 'isFirebaseReady' 
        // del otro script sea 'true'
        yield return new WaitUntil(() => FirebaseManager.isFirebaseReady);

        // Cuando esté lista, guardamos las instancias
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("AuthManager listo para usar Firebase.");
    }


    // --- Funciones de Navegación de Paneles ---

    private void ShowWelcomePanel()
    {
        welcomePanel.SetActive(true);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        ClearErrorText();
    }

    private void ShowLoginPanel()
    {
        welcomePanel.SetActive(false);
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        ClearErrorText();
    }

    private void ShowRegisterPanel()
    {
        welcomePanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        ClearErrorText();
    }

    // --- Funciones de Lógica (Firebase irá aquí) ---

    private void HandleLogin()
    {
        // Leer el texto de los campos de login
        string username = usernameLoginInput.text;
        string password = passwordLoginInput.text;

        Debug.Log($"Intentando login con: Usuario={username}, Pass={password}");

        // --- PRÓXIMAMENTE: Aquí llamaremos a Firebase para el Login ---

        // Campos vacíos
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("El usuario y la contraseña no pueden estar vacíos.");
        }
    }

    private async void HandleRegister()
    {
        ClearErrorText(); // Limpia errores anteriores

        //Recoge los campos del register
        string username = usernameRegisterInput.text;
        string email = emailRegisterInput.text;
        string password = passwordRegisterInput.text;

        // --- 1. Validación de campos (local) ---
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Todos los campos son obligatorios.");
            return;
        }

        if (!email.Contains("@"))
        {
            ShowError("El formato del correo electrónico no es válido.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("La contraseña debe tener al menos 6 caracteres.");
            return;
        }

        Debug.Log("Iniciando registro en Firebase...");
        ShowError("Registrando...");

        try
        {
            // Comprobar si el 'username' ya existe en la BD y si existe mostramos el error
            QuerySnapshot usernameQuery = await db.Collection("usuarios").WhereEqualTo("username", username).Limit(1).GetSnapshotAsync();

            if (usernameQuery.Count > 0)
            {
                ShowError("Ese nombre de usuario ya está en uso.");
                return;
            }

            //Crear el usuario con Firebase Authentication
            // Esto crea el usuario solo con Email y Contraseña
            Task<AuthResult> registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            // Esperamos a que la tarea termine (ya que al ser una bd en la nube puede tardar un poco más)
            await registerTask;

            // Si la creación del usuario falla mediante el sistema Auth, notificamos al usuario
            if (registerTask.IsFaulted)
            {
                FirebaseException firebaseEx = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Error desconocido al registrar.";
                if (errorCode == AuthError.EmailAlreadyInUse)
                {
                    message = "Este correo electrónico ya está registrado.";
                }
                else if (errorCode == AuthError.InvalidEmail)
                {
                    message = "El correo electrónico no es válido.";
                }
                ShowError(message);
                return;
            }

            // Usuario creado con éxito
            string id = registerTask.Result.User.UserId; // Obtenemos el ID del usuario
            Debug.Log($"Usuario creado en Auth con ID: {id}");

            // Diccionario para la ficha del jugador
            Dictionary<string, object> datosPerfil = new Dictionary<string, object>
            {
                { "username", username },
                { "email", email },
                { "role", "Jugador" } // Rol por defecto jugador
            };

            // Diccionario para las estadísticas iniciales
            Dictionary<string, object> estadisticas = new Dictionary<string, object>
            {
                { "total_kills", 0 },
                { "total_wins", 0 },
                { "total_losses", 0 },
                { "best_score", 0 }
            };

            // Añadimos las estadisticas al perfil
            datosPerfil.Add("stats", estadisticas);

            // En la colección usuarios, el usuario con id "id", añade los datos recogidos
            await db.Collection("usuarios").Document(id).SetAsync(datosPerfil);

            Debug.Log("Datos del perfil guardados en Firestore.");
            ShowError($"¡Registro exitoso! Bienvenido, {username}");

            // --- PRÓXIMAMENTE: 
            // Aquí es donde, en lugar de mostrar un error, 
            // cargaríamos la escena del Dashboard.
            // SceneManager.LoadScene("DashboardScene");

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error en el registro: {ex.Message}");
            ShowError("Ha ocurrido un error inesperado.");
        }
    }


    // Errores

    private void ShowError(string message)
    {
        errorText.text = message;
    }

    private void ClearErrorText()
    {
        errorText.text = "";
    }
}