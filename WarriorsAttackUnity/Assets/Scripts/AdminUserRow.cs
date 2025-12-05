using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class AdminUserRow : MonoBehaviour
{
    [Header("UI Fila")]
    public TMP_Text usernameText;
    public TMP_Text emailRoleText;
    public Button editButton;
    public Button deleteButton;

    // Datos del usuario de esta fila
    private string userId;
    private string username;
    private string email;
    private string role;

    // Las llamadas mandando los datos cuando hacemos click en editar y eliminar
    private Action<string, string, string, string> onEditClick;
    private Action<string> onDeleteClick;

    void Start()
    {
        // Al pulsar editar, enviamos los datos al Manager para que rellene el popup
        editButton.onClick.AddListener(() => onEditClick?.Invoke(userId, username, email, role));

        // Al pulsar eliminar enviamos solo el ID
        deleteButton.onClick.AddListener(() => onDeleteClick?.Invoke(userId));
    }

    // Esta función la usa el DashboardManager para rellenar la fila
    public void Configure(string uid, string name, string mail, string rol,
                          Action<string, string, string, string> editCallback,
                          Action<string> deleteCallback)
    {
        userId = uid;
        username = name;
        email = mail;
        role = rol;

        // Rellenamos el texto
        usernameText.text = username;
        emailRoleText.text = $"[{role}] {email}";

        // Guardamos las funciones que nos pasa el Manager
        onEditClick = editCallback;
        onDeleteClick = deleteCallback;
    }
}