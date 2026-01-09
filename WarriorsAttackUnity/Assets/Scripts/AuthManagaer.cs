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
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    [Header("UI Panels")]
    public GameObject welcomePanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Navigation Buttons")]
    public Button goToLoginButton;
    public Button goToRegisterButton;
    public Button backFromLoginButton;
    public Button backFromRegisterButton;

    [Header("Login Fields")]
    public TMP_InputField usernameLoginInput;
    public TMP_InputField passwordLoginInput;
    public Button loginButton;

    [Header("Register Fields")]
    public TMP_InputField usernameRegisterInput;
    public TMP_InputField emailRegisterInput;
    public TMP_InputField passwordRegisterInput;
    public Button registerButton;

    [Header("Feedback")]
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

        StartCoroutine(InitializeFirebaseSequence());
    }

    private IEnumerator InitializeFirebaseSequence()
    {
        yield return new WaitUntil(() => FirebaseManager.isFirebaseReady);

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // --- Navigation Logic ---

    private void ShowWelcomePanel() => TogglePanels(true, false, false);
    private void ShowLoginPanel() => TogglePanels(false, true, false);
    private void ShowRegisterPanel() => TogglePanels(false, false, true);

    private void TogglePanels(bool welcome, bool login, bool register)
    {
        welcomePanel.SetActive(welcome);
        loginPanel.SetActive(login);
        registerPanel.SetActive(register);
        errorText.text = "";
    }

    // --- Auth Logic ---

    private async void HandleLogin()
    {
        errorText.text = "";
        string username = usernameLoginInput.text;
        string password = passwordLoginInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Usuario y contraseña requeridos.");
            return;
        }

        ShowError("Autenticando...");

        try
        {
            // 1. Resolver Email a partir del Username
            QuerySnapshot usernameQuery = await db.Collection("usuarios")
                .WhereEqualTo("username", username)
                .Limit(1)
                .GetSnapshotAsync();

            if (usernameQuery.Count == 0)
            {
                ShowError("Credenciales inválidas.");
                return;
            }

            DocumentSnapshot userDoc = usernameQuery.Documents.FirstOrDefault();
            string email = userDoc.GetValue<string>("email");
            string role = userDoc.GetValue<string>("role");

            // 2. Autenticación contra Firebase Auth
            AuthResult authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = authResult.User;

            Debug.Log($"Login exitoso: {user.UserId} ({role})");
            SceneManager.LoadScene("DashboardScene");
        }
        catch (System.Exception ex)
        {
            HandleFirebaseError(ex);
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
            return;
        }

        if (password.Length < 6)
        {
            ShowError("La contraseña debe tener al menos 6 caracteres.");
            return;
        }

        ShowError("Creando cuenta...");

        try
        {
            // 1. Verificar unicidad del Username
            QuerySnapshot usernameQuery = await db.Collection("usuarios")
                .WhereEqualTo("username", username)
                .Limit(1)
                .GetSnapshotAsync();

            if (usernameQuery.Count > 0)
            {
                ShowError("El nombre de usuario ya existe.");
                return;
            }

            // 2. Crear usuario en Auth
            Task<AuthResult> registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
            await registerTask;

            if (registerTask.IsFaulted) throw registerTask.Exception;

            string userId = registerTask.Result.User.UserId;

            // 3. Crear documento de usuario en Firestore
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

            await db.Collection("usuarios").Document(userId).SetAsync(userProfile);

            ShowLoginPanel();
            usernameLoginInput.text = username;
            ShowError("Cuenta creada. Por favor inicia sesión.");
        }
        catch (System.Exception ex)
        {
            HandleFirebaseError(ex);
        }
    }

    private void HandleFirebaseError(System.Exception ex)
    {
        FirebaseException firebaseEx = ex.GetBaseException() as FirebaseException;
        if (firebaseEx != null)
        {
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