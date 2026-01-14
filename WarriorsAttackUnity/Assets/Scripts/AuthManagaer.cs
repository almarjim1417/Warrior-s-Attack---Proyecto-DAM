using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth; // Para la autenticación
    private FirebaseFirestore db; // Para la base de datos

    [Header("Paneles")]
    public GameObject welcomePanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Botones Navegación")]
    public Button goToLoginButton;
    public Button goToRegisterButton;
    public Button backFromLoginButton;
    public Button backFromRegisterButton;

    [Header("Login")]
    public TMP_InputField usernameLoginInput;
    public TMP_InputField passwordLoginInput;
    public Button loginButton;

    [Header("Registro")]
    public TMP_InputField usernameRegisterInput;
    public TMP_InputField emailRegisterInput;
    public TMP_InputField passwordRegisterInput;
    public Button registerButton;

    [Header("Textos de Error")]
    public TMP_Text errorText;

    void Start()
    {
        ShowWelcomePanel();

        goToLoginButton.onClick.AddListener(ShowLoginPanel);
        goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        backFromLoginButton.onClick.AddListener(ShowWelcomePanel);
        backFromRegisterButton.onClick.AddListener(ShowWelcomePanel);
        loginButton.onClick.AddListener(HandleLogin);
        registerButton.onClick.AddListener(HandleRegister);

        // Llamamos a la corutina para esperar a que firebase esté listo
        StartCoroutine(InitializeFirebaseSequence());
    }

    private IEnumerator InitializeFirebaseSequence()
    {
        // Comprobamos en bucle si firebase está listo pero sin detener el juego (yield return)
        yield return new WaitUntil(() => FirebaseManager.isFirebaseReady);

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // Funciones con TogglePanels para cambiar de pantallas

    private void ShowWelcomePanel() => TogglePanels(true, false, false);
    private void ShowLoginPanel() => TogglePanels(false, true, false);
    private void ShowRegisterPanel() => TogglePanels(false, false, true);

    private void TogglePanels(bool welcome, bool login, bool register)
    {
        welcomePanel.SetActive(welcome);
        loginPanel.SetActive(login);
        registerPanel.SetActive(register);
        // Limpiamos el mensaje de error cada vez que cambiamos de pantalla
        errorText.text = "";
    }

    // Lógica de Usuario

    private async void HandleLogin()
    {
        errorText.text = "";
        string username = usernameLoginInput.text;
        string password = passwordLoginInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Usuario y contraseña requeridos.");
        }
        else
        {
            ShowError("Autenticando...");

            try
            {
                // Primero buscamos el email usando el nombre de usuario
                QuerySnapshot usernameQuery = await db.Collection("usuarios")
                    .WhereEqualTo("username", username)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (usernameQuery.Count == 0)
                {
                    ShowError("Credenciales inválidas.");
                }
                else
                {
                    DocumentSnapshot userDoc = usernameQuery.Documents.FirstOrDefault();
                    string email = userDoc.GetValue<string>("email");
                    string role = userDoc.GetValue<string>("role");

                    // Hacemos login con el email y la contraseña
                    AuthResult authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
                    FirebaseUser user = authResult.User;

                    Debug.Log($"Login correcto: {user.UserId} ({role})");
                    SceneManager.LoadScene("DashboardScene");
                }
            }
            catch (System.Exception ex)
            {
                HandleFirebaseError(ex);
            }
        }
    }

    private async void HandleRegister()
    {
        errorText.text = "";
        string username = usernameRegisterInput.text;
        string email = emailRegisterInput.text;
        string password = passwordRegisterInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Todos los campos son obligatorios.");
        }
        else if (password.Length < 6)
        {
            ShowError("La contraseña debe tener al menos 6 caracteres.");
        }
        else
        {
            ShowError("Creando cuenta...");

            try
            {
                // Comprobamos si ese nombre de usuario ya existe en la base de datos
                QuerySnapshot usernameQuery = await db.Collection("usuarios")
                    .WhereEqualTo("username", username)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (usernameQuery.Count > 0)
                {
                    ShowError("El nombre de usuario ya existe.");
                }
                else
                {
                    // Creamos el usuario en Firebase Authentication
                    Task<AuthResult> registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
                    await registerTask;

                    if (registerTask.IsFaulted) throw registerTask.Exception;

                    string userId = registerTask.Result.User.UserId;

                    // Preparamos los datos iniciales (stats a cero)
                    Dictionary<string, object> userStats = new Dictionary<string, object>
                {
                    { "total_kills", 0 },
                    { "total_wins", 0 },
                    { "total_losses", 0 },
                    { "best_score", 0 }
                };

                    Dictionary<string, object> userProfile = new Dictionary<string, object>
                {
                    { "username", username },
                    { "email", email },
                    { "role", "Jugador" },
                    { "stats", userStats }
                };

                    // Guardamos la ficha del jugador en Firestore
                    await db.Collection("usuarios").Document(userId).SetAsync(userProfile);

                    ShowLoginPanel();
                    usernameLoginInput.text = username;
                    ShowError("Cuenta creada. Por favor inicia sesión.");
                }
            }
            catch (System.Exception ex)
            {
                HandleFirebaseError(ex);
            }
        }
    }

    // Control de errores

    private void HandleFirebaseError(System.Exception ex)
    {
        // Busca el error base y la intenta convertir a excepción
        FirebaseException firebaseEx = ex.GetBaseException() as FirebaseException;
        if (firebaseEx != null)
        {
            // Intenta convertir el firebaseEx en un error "común" para poder identificarlo y tratarlo fácilmente
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.WrongPassword:
                case AuthError.UserNotFound:
                    ShowError("Credenciales incorrectas.");
                    break;
                case AuthError.EmailAlreadyInUse:
                    ShowError("El correo ya está registrado.");
                    break;
                case AuthError.InvalidEmail:
                    ShowError("Correo inválido.");
                    break;
                default:
                    ShowError("Error de conexión.");
                    Debug.LogError($"Firebase Error: {errorCode}");
                    break;
            }
        }
        else
        {
            ShowError("Error desconocido.");
            Debug.LogError(ex.Message);
        }
    }

    private void ShowError(string msg) => errorText.text = msg;
}