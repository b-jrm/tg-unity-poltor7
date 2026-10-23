using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
[System.Serializable] public class ItemEntry { public string id; public int amount; }
[System.Serializable]
public class SaveData
{
    public string scene = "Level1_ElFondo";
    public float health = 100f;
    public float nutrition = 100f;
    public List<ItemEntry> items = new List<ItemEntry>();
    public List<string> flags = new List<string>();
    public bool hasCheckpoint;
    public string checkpointScene;
    public Vector3 checkpointPos;
}
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public SaveData data = new SaveData();
    public float damageTakenMultiplier = 1f; // facil 0.6 / normal 1 / dificil 1.4
    public string menuScene = "MainMenu";
    string SavePath => Path.Combine(Application.persistentDataPath, "poltor7_save.json");

    // Crea el GameManager solo, para poder probar cualquier escena con Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (I == null) new GameObject("GameManager").AddComponent<GameManager>();
    }
    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
    // ---------- flujo de partida ----------
    public void NewGame() { data = new SaveData(); Save(); SceneManager.LoadScene(data.scene); }
    public bool HasSave() => File.Exists(SavePath);
    public void Continue() { Load(); SceneManager.LoadScene(data.scene); }
    public void Retry() { Time.timeScale = 1f; Load(); SceneManager.LoadScene(data.scene); }
    public void GoToMenu() { Time.timeScale = 1f; SceneManager.LoadScene(menuScene); }
    public void CompleteLevel(GameObject player, string nextScene)
    {
        Capture(player);
        data.scene = nextScene;
        data.hasCheckpoint = false;
        Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene);
    }
    public void SetCheckpoint(GameObject player)
    {
        Capture(player);
        data.hasCheckpoint = true;
        data.checkpointScene = SceneManager.GetActiveScene().name;
        data.checkpointPos = player.transform.position;
        Save();
        HUDController.Msg("Punto de control guardado");
    }
    // ---------- estado del jugador ----------
    public void Capture(GameObject player)
    {
        data.health = player.GetComponent<Health>().Current;
        data.nutrition = player.GetComponent<PlayerStats>().nutrition;
        data.items = player.GetComponent<PlayerInventory>().Export();
    }
    public void Restore(GameObject player)
    {
        player.GetComponent<Health>().SetCurrent(Mathf.Max(data.health, 40f));
        player.GetComponent<PlayerStats>().nutrition = data.nutrition;
        player.GetComponent<PlayerInventory>().Import(data.items);
        if (data.hasCheckpoint && data.checkpointScene ==
        SceneManager.GetActiveScene().name)
        {
            player.transform.position = data.checkpointPos;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    // ---------- banderas de historia ----------
    public bool HasFlag(string f) => data.flags.Contains(f);
    public void SetFlag(string f) { if (!data.flags.Contains(f)) data.flags.Add(f); }

    // ---------- guardado ----------
    public void Save() { File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
    public void Load()
    {
        if (File.Exists(SavePath)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
    }
}
