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

    private string userId;
    private string username;
    private string email;
    private string role;

    private Action<string, string, string, string> onEditClick;
    private Action<string> onDeleteClick;

    void Start()
    {
        editButton.onClick.AddListener(() => onEditClick?.Invoke(userId, username, email, role));
        deleteButton.onClick.AddListener(() => onDeleteClick?.Invoke(userId));
    }

    public void Configure(string uid, string name, string mail, string rol,
                          Action<string, string, string, string> editCallback,
                          Action<string> deleteCallback)
    {
        userId = uid;
        username = name;
        email = mail;
        role = rol;

        usernameText.text = username;
        emailRoleText.text = $"[{role}] {email}";

        onEditClick = editCallback;
        onDeleteClick = deleteCallback;
    }
}