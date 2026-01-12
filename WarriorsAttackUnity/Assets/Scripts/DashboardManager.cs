using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class DashboardManager : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject playerPanel;
    public GameObject adminPanel;

    [Header("Ranking UI")]
    public GameObject rankingPanel;
    public Transform rankingContainer;
    public GameObject rankingRowPrefab;
    public Button openRankingButton;
    public Button closeRankingButton;

    [Header("UI Jugador")]
    public TMP_Text playerWelcomeText;
    public TMP_Text playerStatsText;
    public Button playButton;
    public Button logoutButton;

    [Header("Admin Lista")]
    public Transform adminContainer;
    public GameObject adminRowPrefab;
    public TMP_Text adminWelcomeText;
    public Button adminLogoutButton;
    public TMP_InputField searchInput;

    [Header("Admin Popups")]
    public GameObject confirmationPopup;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;

    public GameObject editUserPopup;
    public TMP_InputField editUsernameInput;
    public TMP_InputField editEmailInput;
    public Button resetPasswordButton;
    public TMP_Dropdown editRoleDropdown;
    public Button saveChangesButton;
    public Button cancelEditButton;
    public TMP_Text resetFeedbackText;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string currentUserIdSelected;

    void Start()
    {
        // Ocultamos todo al empezar para que no se vea feo mientras carga
        playerPanel.SetActive(false);
        adminPanel.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (editUserPopup != null) editUserPopup.SetActive(false);

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Si no hay usuario logueado, vuelve al Login
        if (auth.CurrentUser == null)
        {
            SceneManager.LoadScene("AuthScene");
            return;
        }

        // CONFIGURAR BOTONES
        logoutButton.onClick.AddListener(LogOut);
        adminLogoutButton.onClick.AddListener(LogOut);

        if (playButton != null) playButton.onClick.AddListener(GoToGameScene);

        if (openRankingButton != null) openRankingButton.onClick.AddListener(LoadAndShowRanking);
        if (closeRankingButton != null) closeRankingButton.onClick.AddListener(CloseRanking);

        // Botones de los Popups de Admin
        confirmDeleteButton.onClick.AddListener(ConfirmDeleteUser);
        cancelDeleteButton.onClick.AddListener(ClosePopups);
        saveChangesButton.onClick.AddListener(SaveChangesUser);
        cancelEditButton.onClick.AddListener(ClosePopups);
        if (resetPasswordButton != null) resetPasswordButton.onClick.AddListener(SendResetEmail);

        // Si escribimos en el buscador, recargamos la lista
        if (searchInput != null) searchInput.onValueChanged.AddListener(delegate { LoadAllUsers(); });

        LoadUserData();
    }

    // CARGAMOS DATOS DEL USUARIO ACTUAL

    private async void LoadUserData()
    {
        string uid = auth.CurrentUser.UserId;
        DocumentSnapshot doc = await db.Collection("usuarios").Document(uid).GetSnapshotAsync();

        if (doc.Exists)
        {
            string username = doc.GetValue<string>("username");
            string role = doc.GetValue<string>("role");

            // Dependiendo del rol, mostramos un panel u otro
            if (role == "Admin")
            {
                ShowAdmin(username);
            }
            else
            {
                Dictionary<string, object> stats = doc.GetValue<Dictionary<string, object>>("stats");
                ShowPlayer(username, stats);
            }
        }
    }

    private void ShowPlayer(string name, Dictionary<string, object> stats)
    {
        playerPanel.SetActive(true);
        adminPanel.SetActive(false);

        playerWelcomeText.text = "Bienvenido, " + name;

        // Sacamos las estadísticas del diccionario
        long kills = System.Convert.ToInt64(stats["total_kills"]);
        long wins = System.Convert.ToInt64(stats["total_wins"]);
        long score = System.Convert.ToInt64(stats["best_score"]);

        playerStatsText.text = $"Total de Kills: {kills}\n\n" +
                               $"Total de Victorias: {wins}\n\n" +
                               $"Puntuación Total: {score}";
    }

    private void ShowAdmin(string name)
    {
        adminPanel.SetActive(true);
        playerPanel.SetActive(false);
        adminWelcomeText.text = "Panel de Administrador\nHola, " + name;

        LoadAllUsers();
    }

    // FUNCIONES DE ADMINISTRADOR

    private async void LoadAllUsers()
    {
        // Borramos la lista anterior
        foreach (Transform child in adminContainer) Destroy(child.gameObject);

        try
        {
            QuerySnapshot snapshot = await db.Collection("usuarios").GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                string username = doc.GetValue<string>("username");

                // Filtro del buscador
                if (searchInput != null && !string.IsNullOrEmpty(searchInput.text))
                {
                    if (!username.ToLower().Contains(searchInput.text.ToLower())) continue;
                }

                // Instanciamos un prefab de la fila
                GameObject row = Instantiate(adminRowPrefab, adminContainer);

                // Configuramos la fila con los datos y los poups de los botones
                row.GetComponent<AdminUserRow>().Configure(
                    doc.Id,
                    username,
                    doc.GetValue<string>("email"),
                    doc.GetValue<string>("role"),
                    OpenEditPopup,
                    OpenDeletePopup
                );
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error cargando lista: " + ex.Message);
        }
    }

    private void OpenDeletePopup(string uid)
    {
        // Para no poder borrarse a si mismo compara el uid con el userId actual
        if (uid != auth.CurrentUser.UserId)
        {
            currentUserIdSelected = uid;
            confirmationPopup.SetActive(true);
        }
    }

    private async void ConfirmDeleteUser()
    {
        // Si confirma el delete, borra al usuario, cierra los popups y refresca la lista
        if (!string.IsNullOrEmpty(currentUserIdSelected))
        {
            await db.Collection("usuarios").Document(currentUserIdSelected).DeleteAsync();
            ClosePopups();
            LoadAllUsers();
        }
    }

    private void OpenEditPopup(string uid, string name, string email, string role)
    {
        currentUserIdSelected = uid;
        resetFeedbackText.text = "";

        // Cargamos los datos por defecto
        editUsernameInput.text = name;
        editEmailInput.text = email;
        if (role == "Admin") editRoleDropdown.value = 1;
        else editRoleDropdown.value = 0;

        editUserPopup.SetActive(true);
    }

    private async void SaveChangesUser()
    {
        if (!string.IsNullOrEmpty(currentUserIdSelected))
        {
            // Preparamos solo los datos que queremos cambiar
            Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "username", editUsernameInput.text },
            { "email", editEmailInput.text },
            { "role", editRoleDropdown.options[editRoleDropdown.value].text }
        };

            await db.Collection("usuarios").Document(currentUserIdSelected).UpdateAsync(updates);
            ClosePopups();
            LoadAllUsers();
        }
    }

    private void SendResetEmail()
    {
        string emailTarget = editEmailInput.text;
        if (!string.IsNullOrEmpty(emailTarget))
        {
            // Enviamos el correo de recuperación de contraseña
            auth.SendPasswordResetEmailAsync(emailTarget).ContinueWithOnMainThread(task => {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Error al enviar correo.");
                    resetFeedbackText.text = "❌ Error al enviar";
                }
                else
                {
                    Debug.Log("Correo enviado.");
                    resetFeedbackText.text = "✅ Correo enviado";
                }
            });
        }
    }

    private void ClosePopups()
    {
        if (confirmationPopup) confirmationPopup.SetActive(false);
        if (editUserPopup) editUserPopup.SetActive(false);
        currentUserIdSelected = "";
    }

    // RANKING

    private async void LoadAndShowRanking()
    {
        playerPanel.SetActive(false);
        adminPanel.SetActive(false);
        rankingPanel.SetActive(true);

        // Limpiamos la tabla
        foreach (Transform child in rankingContainer) Destroy(child.gameObject);

        // Pedimos los 10 mejores jugadores ordenados por puntuación
        QuerySnapshot snapshot = await db.Collection("usuarios")
                                         .OrderByDescending("stats.best_score")
                                         .Limit(10)
                                         .GetSnapshotAsync();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> stats = doc.GetValue<Dictionary<string, object>>("stats");

            GameObject row = Instantiate(rankingRowPrefab, rankingContainer);
            row.GetComponent<RankingRow>().SetData(
                doc.GetValue<string>("username"),
                stats["best_score"].ToString()
            );
        }
    }

    private void CloseRanking()
    {
        rankingPanel.SetActive(false);
        playerPanel.SetActive(true);
    }

    // NAVEGACIÓN

    private void GoToGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LogOut()
    {
        auth.SignOut();
        SceneManager.LoadScene("AuthScene");
    }
}