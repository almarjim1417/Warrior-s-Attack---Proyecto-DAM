using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

//Firebase
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase;
using UnityEngine.SceneManagement;


public class AuthManager : MonoBehaviour
{

    private FirebaseAuth auth;
    private FirebaseFirestore db;

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
        // Mostrar la pantalla principal
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


    // Funciones de Navegación

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

    // Funciones de Lógica

    private async void HandleLogin()
    {
        ClearErrorText(); // Limpia errores anteriores

        // Leer el texto de los campos de login
        string username = usernameLoginInput.text;
        string password = passwordLoginInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("El usuario y la contraseña no pueden estar vacíos.");
            return;
        }

        Debug.Log("Iniciando Login...");
        ShowError("Iniciando sesión...");

        try
        {
            // Buscamos el usuario con el usuario introducido
            QuerySnapshot usernameQuery = await db.Collection("usuarios").WhereEqualTo("username", username).Limit(1).GetSnapshotAsync();

            if (usernameQuery.Count == 0)
            {
                // Si no devuelve ningún usuario
                ShowError("Nombre de usuario o contraseña incorrectos.");
            }
            else
            {

                // Obtenemos el documento/usuario correspondiente a ese nombre de usuario
                DocumentSnapshot userDoc = usernameQuery.Documents.FirstOrDefault();
                string email = userDoc.GetValue<string>("email");

                Debug.Log($"LOGIN[mail: '{email}' y Pass: '{password}']");

                // Iniciar sesión usando el email recogido con el nombre de usuario y la contraseña
                AuthResult authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);

                // Login correcto
                FirebaseUser user = authResult.User;
                Debug.Log($"Login exitoso: {user.Email} (UID: {user.UserId})");

                //Comprobar ROL
                string role = userDoc.GetValue<string>("role");

                if (role == "Admin")
                {
                    Debug.Log("Acceso de Administrador Concedido.");
                    ShowError("¡Bienvenido Admin!");
                }
                else
                {
                    Debug.Log("Acceso de Jugador Concedido.");
                    ShowError($"¡Bienvenido {username}!");
                }

                // Cargar la escena
                SceneManager.LoadScene("DashboardScene");
            }
        }
        catch (System.Exception ex)
        {
            // Intentamos convertir el error en un error específico de Firebase
            FirebaseException firebaseEx = ex.GetBaseException() as FirebaseException;

            if (firebaseEx != null)
            {
                // Si es un error de Firebase, leemos su código
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                
                
                Debug.LogError($"[DEBUG] Código de error de Firebase: {errorCode.ToString()} ({firebaseEx.ErrorCode})");


                // Comprobamos si el error fue por contraseña incorrecta
                if (errorCode == AuthError.WrongPassword)
                {
                    ShowError("Nombre de usuario o contraseña incorrectos.");
                }
                else
                {
                    Debug.LogError($"Error de Firebase en el login: {firebaseEx.Message}");
                    ShowError("Error al conectar. Inténtalo más tarde.");
                }
            }
            else
            {
                Debug.LogError($"Error en el login: {ex.Message}");
                ShowError("Ha ocurrido un error inesperado.");
            }

        }
    }

    private async void HandleRegister()
    {
        ClearErrorText(); // Limpia errores anteriores

        //Recoge los campos del register
        string username = usernameRegisterInput.text;
        string email = emailRegisterInput.text;
        string password = passwordRegisterInput.text;

        // Validación de campos
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

            // Crear el usuario con Firebase Authentication
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

            ShowLoginPanel();
            usernameLoginInput.text = username;

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