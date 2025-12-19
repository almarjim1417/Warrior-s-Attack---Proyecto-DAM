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
    [Header("--- PANELES PRINCIPALES ---")]
    public GameObject playerPanel;
    public GameObject adminPanel;

    [Header("--- RANKING UI ---")]
    public GameObject rankingPanel;
    public Transform rankingContainer;
    public GameObject rankingRowPrefab;
    public Button openRankingButton;
    public Button closeRankingButton;

    [Header("--- UI JUGADOR ---")]
    public TMP_Text playerWelcomeText;
    public TMP_Text playerStatsText;
    public Button playButton;
    public Button logoutButton;

    [Header("--- ADMIN LISTA ---")]
    public Transform adminContainer;
    public GameObject adminRowPrefab;
    public TMP_Text adminWelcomeText;
    public Button adminLogoutButton;
    public TMP_InputField searchInput;

    [Header("--- ADMIN POPUPS ---")]
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
        playerPanel.SetActive(false);
        adminPanel.SetActive(false);

        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (editUserPopup != null) editUserPopup.SetActive(false);

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            SceneManager.LoadScene("AuthScene");
            return;
        }

        // Botones básicos (Logout)
        logoutButton.onClick.AddListener(LogOut);
        adminLogoutButton.onClick.AddListener(LogOut);

        // Boton jugar
        if (playButton != null) playButton.onClick.AddListener(GoToGameScene);

        // Botones de Ranking
        if (openRankingButton != null) openRankingButton.onClick.AddListener(LoadAndShowRanking);
        if (closeRankingButton != null) closeRankingButton.onClick.AddListener(CloseRanking);

        // Listeners Popups
        confirmDeleteButton.onClick.AddListener(ConfirmDeleteUser);
        cancelDeleteButton.onClick.AddListener(ClosePopups);
        saveChangesButton.onClick.AddListener(SaveChangesUser);
        cancelEditButton.onClick.AddListener(ClosePopups);
        if (resetPasswordButton != null) resetPasswordButton.onClick.AddListener(SendResetEmail);

        // Buscador
        if (searchInput != null) searchInput.onValueChanged.AddListener(delegate { LoadAllUsers(); });

        LoadUserData();
    }

    private async void LoadUserData()
    {
        string uid = auth.CurrentUser.UserId;
        DocumentSnapshot doc = await db.Collection("usuarios").Document(uid).GetSnapshotAsync();

        if (!doc.Exists)
        {
            Debug.LogError("Usuario sin datos en Firestore.");
            return;
        }

        string username = doc.GetValue<string>("username");
        string role = doc.GetValue<string>("role");

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

    private void ShowPlayer(string name, Dictionary<string, object> stats)
    {
        playerPanel.SetActive(true);
        adminPanel.SetActive(false);

        playerWelcomeText.text = "Bienvenido, " + name;

        long kills = System.Convert.ToInt64(stats["total_kills"]);
        long wins = System.Convert.ToInt64(stats["total_wins"]);
        long score = System.Convert.ToInt64(stats["best_score"]);

        playerStatsText.text = $"Total de Kills: {kills}\n\n" +
                               $"Total de Victorias: {wins}\n\n" +
                               $"Mejor Puntuación: {score}";
    }

    private void ShowAdmin(string name)
    {
        adminPanel.SetActive(true);
        playerPanel.SetActive(false);
        adminWelcomeText.text = "Panel de Administrador\nHola, " + name;

        LoadAllUsers();
    }

    private async void LoadAllUsers()
    {
        foreach (Transform child in adminContainer) Destroy(child.gameObject);

        try
        {
            QuerySnapshot snapshot = await db.Collection("usuarios").GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                string uid = doc.Id;
                string username = doc.GetValue<string>("username");
                string email = doc.GetValue<string>("email");
                string role = doc.GetValue<string>("role");

                // Filtro básico de buscador
                if (searchInput != null && !string.IsNullOrEmpty(searchInput.text))
                {
                    if (!username.ToLower().Contains(searchInput.text.ToLower())) continue;
                }

                GameObject row = Instantiate(adminRowPrefab, adminContainer);

                // Pasamos las funciones OpenEdit y OpenDelete como parámetros
                row.GetComponent<AdminUserRow>().Configure(uid, username, email, role, OpenEditPopup, OpenDeletePopup);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error cargando usuarios: {ex.Message}");
        }
    }

    // ABRIR CONFIRMACIÓN BORRAR
    private void OpenDeletePopup(string uid)
    {
        if (uid == auth.CurrentUser.UserId) { Debug.LogError("No puedes borrarte a ti mismo."); return; }

        currentUserIdSelected = uid;
        confirmationPopup.SetActive(true);
    }

    // EJECUTAR BORRADO
    private async void ConfirmDeleteUser()
    {
        if (string.IsNullOrEmpty(currentUserIdSelected)) return;

        try
        {
            await db.Collection("usuarios").Document(currentUserIdSelected).DeleteAsync();
            Debug.Log("Usuario eliminado.");
            ClosePopups();
            LoadAllUsers();
        }
        catch (System.Exception ex) { Debug.LogError("Error al borrar: " + ex.Message); }
    }

    // ABRIR EDITOR
    private void OpenEditPopup(string uid, string name, string email, string role)
    {
        currentUserIdSelected = uid;

        resetFeedbackText.text = "";
        // Rellenamos el formulario con los datos actuales
        editUsernameInput.text = name;
        editEmailInput.text = email;

        // Ajustar el Dropdown
        if (role == "Admin") editRoleDropdown.value = 1; // Asumiendo que Admin es el índice 1
        else editRoleDropdown.value = 0; // Jugador

        editUserPopup.SetActive(true);
    }

    // GUARDAR CAMBIOS
    private async void SaveChangesUser()
    {
        if (string.IsNullOrEmpty(currentUserIdSelected)) return;

        string newName = editUsernameInput.text;
        string newEmail = editEmailInput.text;
        string newRole = editRoleDropdown.options[editRoleDropdown.value].text;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "username", newName },
            { "email", newEmail },
            { "role", newRole }
        };

        try
        {
            await db.Collection("usuarios").Document(currentUserIdSelected).UpdateAsync(updates);
            Debug.Log("Usuario actualizado.");
            ClosePopups();
            LoadAllUsers();
        }
        catch (System.Exception ex) { Debug.LogError("Error al actualizar: " + ex.Message); }
    }

    private void SendResetEmail()
    {
        // Cogemos el email del usuario
        string emailTarget = editEmailInput.text;

        if (string.IsNullOrEmpty(emailTarget))
        {
            Debug.LogError("El campo email está vacío.");
            return;
        }

        // Enviamos la orden a Firebase Auth
        auth.SendPasswordResetEmailAsync(emailTarget).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Error al enviar correo de reset.");
            }
            else
            {
                Debug.Log($"Correo de recuperación enviado a {emailTarget}");
            }

            auth.SendPasswordResetEmailAsync(emailTarget).ContinueWithOnMainThread(task => {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("Error al enviar correo de reset.");
                }
                else
                {
                    Debug.Log($"Correo de recuperación enviado a {emailTarget}");

                    if (resetFeedbackText != null)
                    {
                        resetFeedbackText.text = "✅ Correo enviado correctamente";
                    }
                }
            });
        });
    }

    private void ClosePopups()
    {
        confirmationPopup.SetActive(false);
        editUserPopup.SetActive(false);
        currentUserIdSelected = "";
    }



    private async void LoadAndShowRanking()
    {
        Debug.Log($"[DEBUG] Intentando activar el objeto llamado: {rankingPanel.name}");
        playerPanel.SetActive(false);
        adminPanel.SetActive(false);
        rankingPanel.SetActive(true);


        // Limpiar lista anterior
        foreach (Transform child in rankingContainer)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Cargando Ranking...");

        try
        {
            QuerySnapshot snapshot = await db.Collection("usuarios")
                                             .OrderByDescending("stats.best_score")
                                             .Limit(10)
                                             .GetSnapshotAsync();

            Debug.Log($"[RANKING] Consulta terminada. Se han encontrado {snapshot.Count} usuarios.");

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                string name = doc.GetValue<string>("username");
                Dictionary<string, object> stats = doc.GetValue<Dictionary<string, object>>("stats");
                string score = stats["best_score"].ToString();

                GameObject row = Instantiate(rankingRowPrefab, rankingContainer);
                row.GetComponent<RankingRow>().SetData(name, score);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error al cargar ranking: {ex.Message}");
        }
    }

    // Función para empezar partida
    private void GoToGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void CloseRanking()
    {
        rankingPanel.SetActive(false);
        playerPanel.SetActive(true);
    }

    public void LogOut()
    {
        auth.SignOut();
        SceneManager.LoadScene("AuthScene");
    }
}