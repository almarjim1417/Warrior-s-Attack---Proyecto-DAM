using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class AdminUserRow : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text usernameText;
    public TMP_Text emailRoleText;
    public Button editButton;
    public Button deleteButton;

    // Datos de este usuario
    private string userId;
    private string username;
    private string email;
    private string role;

    // Acciones para editar y borrar que nos pasarán desde fuera
    private Action<string, string, string, string> onEditClick;
    private Action<string> onDeleteClick;

    void Start()
    {
        // Al pulsar los botones, llamamos a las funciones correspondientes enviando los datos
        editButton.onClick.AddListener(() => onEditClick?.Invoke(userId, username, email, role));
        deleteButton.onClick.AddListener(() => onDeleteClick?.Invoke(userId));
    }

    // Esta función se llama para rellenar la fila con la información
    public void Configure(string uid, string name, string mail, string rol,
                          Action<string, string, string, string> editCallback,
                          Action<string> deleteCallback)
    {
        // Guardamos los datos
        userId = uid;
        username = name;
        email = mail;
        role = rol;

        // Ponemos el texto en la pantalla
        usernameText.text = username;
        emailRoleText.text = $"[{role}] {email}";

        // Guardamos las funciones que usaremos al hacer click
        onEditClick = editCallback;
        onDeleteClick = deleteCallback;
    }
}