using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static bool isFirebaseReady = false;

    [Header("Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D textCursor;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        SetDefaultCursor();
        StartCoroutine(InitializeFirebaseSequence());
    }

    private IEnumerator InitializeFirebaseSequence()
    {
        var checkTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => checkTask.IsCompleted);

        if (checkTask.Result == Firebase.DependencyStatus.Available)
        {
            Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = true;
            Debug.Log("Firebase Initialized Successfully.");
        }
        else
        {
            Debug.LogError($"Firebase Dependency Error: {checkTask.Result}");
            isFirebaseReady = false;
        }
    }

    // --- CURSOR LOGIC ---

    public void SetDefaultCursor()
    {
        if (defaultCursor != null)
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }

    public void SetTextCursor()
    {
        if (textCursor != null)
        {
            Vector2 hotspot = new Vector2(textCursor.width / 2, textCursor.height / 2);
            Cursor.SetCursor(textCursor, hotspot, CursorMode.Auto);
        }
    }

    // --- STATS LOGIC ---

    public async void ActualizarEstadistica(string type, int amount)
    {
        if (!isFirebaseReady || auth.CurrentUser == null) return;

        string userId = auth.CurrentUser.UserId;
        DocumentReference userDoc = db.Collection("usuarios").Document(userId);

        string fieldPath = GetFieldPath(type);
        if (string.IsNullOrEmpty(fieldPath)) return;

        try
        {
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { fieldPath, FieldValue.Increment(amount) }
            };

            await userDoc.UpdateAsync(updates);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update stats: {ex.Message}");
        }
    }

    public void ActualizarScore(int amount)
    {
        ActualizarEstadistica("score", amount);
    }

    private string GetFieldPath(string type)
    {
        switch (type)
        {
            case "kill": return "stats.total_kills";
            case "win": return "stats.total_wins";
            case "loss": return "stats.total_losses";
            case "score": return "stats.best_score";
            default: return "";
        }
    }
}