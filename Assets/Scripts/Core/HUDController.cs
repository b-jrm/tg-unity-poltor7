
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Barras de estado")]
    public Slider healthBar;
    public Slider nutritionBar;

    [Header("Textos del HUD")]
    public TMP_Text promptText;
    public TMP_Text messageText;
    public TMP_Text ammoText;
    public TMP_Text medkitText;

    [Header("Panel de Game Over")]
    public GameObject gameOverPanel;

    [Header("Inventario visual")]
    [Tooltip("Contenedor horizontal donde aparecerán los iconos.")]
    public Transform itemIconsContainer;

    [Tooltip("Prefab con un componente Image y un texto TMP para la cantidad.")]
    public GameObject itemIconPrefab;

    [Tooltip("Arrastra aquí todos los ItemData de Assets/Data/Items.")]
    public ItemData[] allItems;

    private Health hp;
    private PlayerStats stats;
    private PlayerInventory inv;

    private float messageUntil;

    private readonly List<GameObject> spawnedIcons =
        new List<GameObject>();

    public static void Msg(string message)
    {
        if (Instance != null)
            Instance.ShowMessage(message);
    }

    public static void Prompt(string message)
    {
        if (Instance != null && Instance.promptText != null)
            Instance.promptText.text = message;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError(
                "HUDController: No se encontró un objeto con Tag Player."
            );
            return;
        }

        hp = player.GetComponent<Health>();
        stats = player.GetComponent<PlayerStats>();
        inv = player.GetComponent<PlayerInventory>();

        if (hp == null || stats == null || inv == null)
        {
            Debug.LogError(
                "HUDController: Ike necesita Health, PlayerStats " +
                "y PlayerInventory."
            );
            return;
        }

        hp.onDeath.AddListener(ShowGameOver);

        if (messageText != null)
            messageText.text = "";

        if (promptText != null)
            promptText.text = "";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Actualizar iconos cada vez que cambie el inventario.
        inv.OnChanged += RefreshItemIcons;

        // Mostrar el inventario que Ike ya tenga al iniciar.
        RefreshItemIcons();

        UpdateHUD();
    }

    private void Update()
    {
        UpdateHUD();

        if (messageText != null &&
            messageText.text != "" &&
            Time.time > messageUntil)
        {
            messageText.text = "";
        }
    }

    private void UpdateHUD()
    {
        if (hp != null && healthBar != null)
        {
            healthBar.value = hp.maxHealth > 0f
                ? hp.Current / hp.maxHealth
                : 0f;
        }

        if (stats != null && nutritionBar != null)
        {
            nutritionBar.value = stats.maxNutrition > 0f
                ? stats.nutrition / stats.maxNutrition
                : 0f;
        }

        if (inv != null && medkitText != null)
        {
            medkitText.text = "Botiquines: " + inv.Count("medkit");
        }
    }

    public void RefreshItemIcons()
    {
        if (itemIconsContainer == null || itemIconPrefab == null)
            return;

        // Eliminar los iconos anteriores.
        foreach (GameObject icon in spawnedIcons)
        {
            if (icon != null)
                Destroy(icon);
        }

        spawnedIcons.Clear();

        if (inv == null || allItems == null)
            return;

        // Recorrer los objetos definidos para POLTOR7.
        foreach (ItemData item in allItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
                continue;

            int amount = inv.Count(item.id);

            // Mostrar únicamente los objetos que Ike posee.
            if (amount <= 0)
                continue;

            if (item.icon == null)
            {
                Debug.LogWarning(
                    "El objeto " + item.displayName +
                    " no tiene un icono asignado."
                );
                continue;
            }

            GameObject iconObject = Instantiate(
                itemIconPrefab,
                itemIconsContainer
            );

            Image iconImage = iconObject.GetComponent<Image>();

            if (iconImage != null)
                iconImage.sprite = item.icon;

            // Si el prefab contiene un TMP para la cantidad,
            // mostrarla automáticamente.
            TMP_Text amountText =
                iconObject.GetComponentInChildren<TMP_Text>(true);

            if (amountText != null)
                amountText.text = amount > 1 ? "x" + amount : "";

            // Nombre accesible para identificarlo en la jerarquía.
            iconObject.name = "Item_" + item.id;

            spawnedIcons.Add(iconObject);
        }
    }

    public void ShowMessage(string message, float seconds = 3f)
    {
        if (messageText == null)
            return;

        messageText.text = message;
        messageUntil = Time.time + seconds;
    }

    private void ShowGameOver()
    {
        PlayerController.InputEnabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        if (GameManager.I != null)
            GameManager.I.Retry();
    }

    public void OnMenuClicked()
    {
        if (GameManager.I != null)
            GameManager.I.GoToMenu();
    }

    private void OnDestroy()
    {
        if (inv != null)
            inv.OnChanged -= RefreshItemIcons;

        if (hp != null)
            hp.onDeath.RemoveListener(ShowGameOver);

        if (Instance == this)
            Instance = null;
    }
}
