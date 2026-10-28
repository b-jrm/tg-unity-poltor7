
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }
    public Slider healthBar, nutritionBar;
    public TMP_Text promptText, messageText, ammoText, medkitText;
    public GameObject gameOverPanel;
    Health hp; PlayerStats stats; PlayerInventory inv; WeaponManager weapons;
    float messageUntil;
    // Atajos estaticos seguros (no fallan si no hay HUD en la escena)
    public static void Msg(string t) { if (Instance != null) Instance.ShowMessage(t); }
    public static void Prompt(string t) { if (Instance != null) Instance.promptText.text = t; }
    void Awake() { Instance = this; }
    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        hp = player.GetComponent<Health>();
        stats = player.GetComponent<PlayerStats>();
        inv = player.GetComponent<PlayerInventory>();
        weapons = player.GetComponentInChildren<WeaponManager>();
        hp.onDeath.AddListener(ShowGameOver);
        messageText.text = "";
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    void Update()
    {
        healthBar.value = hp.Current / hp.maxHealth;
        nutritionBar.value = stats.nutrition / stats.maxNutrition;
        medkitText.text = "Botiquines: " + inv.Count("medkit");
        var w = weapons != null ? weapons.Current : null;
        ammoText.text = (w != null && w.UsesAmmo) ? w.Mag + " / " + inv.Count(w.ammoItemId) : "";
        if (messageText.text != "" && Time.time > messageUntil) messageText.text = "";
    }
    public void ShowMessage(string t, float seconds = 3f)
    {
        messageText.text = t;
        messageUntil = Time.time + seconds;
    }
    void ShowGameOver()
    {
        PlayerController.InputEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }
    // Se asignan a los botones del panel Game Over desde el Inspector
    public void OnRetryClicked() { GameManager.I.Retry(); }
    public void OnMenuClicked() { GameManager.I.GoToMenu(); }
}