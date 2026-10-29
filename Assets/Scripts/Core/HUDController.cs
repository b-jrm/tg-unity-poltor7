using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el HUD: salud, nutrición, munición, botiquines, mensajes e iconos de objetos clave.
/// Unity 6 (uGUI + TextMeshPro).
/// </summary>
[DisallowMultipleComponent]
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Barras (Slider 0-1)")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider nutritionSlider;
    [SerializeField] private Color healthFullColor = new Color(0.25f, 0.85f, 0.35f);
    [SerializeField] private Color healthLowColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] private Color nutritionFullColor = new Color(0.95f, 0.70f, 0.20f);
    [SerializeField] private Color nutritionLowColor = new Color(0.55f, 0.30f, 0.10f);
    [Tooltip("Velocidad de animación de las barras (fracción por segundo).")]
    [SerializeField] private float barSmoothSpeed = 2f;

    [Header("Textos")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text medkitText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string ammoFormat = "{0} / {1}";
    [SerializeField] private string medkitFormat = "Botiquines: {0}";
    [SerializeField] private float defaultMessageDuration = 3f;
    [SerializeField] private float messageFadeDuration = 0.5f;

    [Header("Objetos clave")]
    [SerializeField] private RectTransform keyItemContainer;
    [Tooltip("Opcional. Si es null se crea un Image simple por cada objeto.")]
    [SerializeField] private Image keyItemSlotPrefab;
    [SerializeField] private Vector2 keyItemSize = new Vector2(64f, 64f);

    private float healthTarget = 1f;
    private float nutritionTarget = 1f;
    private Image healthFill;
    private Image nutritionFill;
    private Coroutine messageRoutine;
    private readonly Dictionary<string, Image> keyItems = new Dictionary<string, Image>();
    private Health playerHealth;
    private PlayerStats playerStats;

    // ---------------------------------------------------------------- Unity
    public static void Msg(string t) { if (Instance != null) Instance.ShowMessage(t); }

    public static void Prompt(string t) { if (Instance != null) Instance.messageText.text = t; }

    private void Awake()
    {
        //Debug.Log($"healthSlider: {healthSlider}");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (healthSlider != null && healthSlider.fillRect != null)
            healthFill = healthSlider.fillRect.GetComponent<Image>();
        if (nutritionSlider != null && nutritionSlider.fillRect != null)
            nutritionFill = nutritionSlider.fillRect.GetComponent<Image>();

        if (healthSlider != null) healthSlider.value = healthTarget;
        if (nutritionSlider != null) nutritionSlider.value = nutritionTarget;
        ApplyBarColors();

        if (messageText != null) messageText.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        float step = barSmoothSpeed * Time.unscaledDeltaTime;
        if (healthSlider != null)
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, healthTarget, step);
        if (nutritionSlider != null)
            nutritionSlider.value = Mathf.MoveTowards(nutritionSlider.value, nutritionTarget, step);
        ApplyBarColors();
    }

    private void ApplyBarColors()
    {
        if (healthFill != null)
            healthFill.color = Color.Lerp(healthLowColor, healthFullColor, healthSlider.value);
        if (nutritionFill != null)
            nutritionFill.color = Color.Lerp(nutritionLowColor, nutritionFullColor, nutritionSlider.value);
    }

    // ------------------------------------------------------------ API pública

    /// <summary>Actualiza la barra de salud.</summary>
    public void SetHealth(float current, float max)
    {
        //Debug.Log($"current: {current}, max: {max}");
        healthTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        //Debug.Log($"healthTarget: {healthTarget}");
    }

    /// <summary>Actualiza la barra de nutrición.</summary>
    public void SetNutrition(float current, float max)
    {
        nutritionTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    /// <summary>Muestra munición. Con reserve &lt; 0 solo se muestra el cargador.</summary>
    public void SetAmmo(int inMagazine, int reserve)
    {
        if (ammoText == null) return;
        ammoText.text = reserve < 0
            ? inMagazine.ToString()
            : string.Format(ammoFormat, inMagazine, reserve);
    }

    /// <summary>Oculta o muestra el texto de munición (p. ej. sin arma equipada).</summary>
    public void SetAmmoVisible(bool visible)
    {
        if (ammoText != null) ammoText.gameObject.SetActive(visible);
    }

    public void SetMedkits(int count)
    {
        if (medkitText != null)
            medkitText.text = string.Format(medkitFormat, Mathf.Max(0, count));
    }

    /// <summary>Muestra un mensaje temporal. duration &lt;= 0 usa la duración por defecto.</summary>
    public void ShowMessage(string message, float duration = -1f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(message, duration > 0f ? duration : defaultMessageDuration));
    }

    public void ClearMessage()
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = null;
        messageText.alpha = 0f;
    }

    /// <summary>Agrega (o actualiza) un icono de objeto clave.</summary>
    public void AddKeyItem(string id, Sprite icon)
    {
        if (keyItemContainer == null || string.IsNullOrEmpty(id)) return;

        if (keyItems.TryGetValue(id, out Image existing))
        {
            existing.sprite = icon;
            return;
        }

        Image img;
        if (keyItemSlotPrefab != null)
        {
            img = Instantiate(keyItemSlotPrefab, keyItemContainer);
            img.name = "KeyItem_" + id;
        }
        else
        {
            var go = new GameObject("KeyItem_" + id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(keyItemContainer, false);
            img = go.GetComponent<Image>();
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = keyItemSize.x;
            le.preferredHeight = keyItemSize.y;
        }

        img.sprite = icon;
        img.preserveAspect = true;
        img.raycastTarget = false;
        keyItems.Add(id, img);
    }

    public void RemoveKeyItem(string id)
    {
        if (keyItems.TryGetValue(id, out Image img))
        {
            if (img != null) Destroy(img.gameObject);
            keyItems.Remove(id);
        }
    }

    public bool HasKeyItem(string id) => keyItems.ContainsKey(id);

    public void ClearKeyItems()
    {
        foreach (var kv in keyItems)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        keyItems.Clear();
    }

    // --------------------------------------------------------------- Interno

    private IEnumerator MessageRoutine(string message, float duration)
    {
        messageText.text = message;
        messageText.alpha = 1f;

        yield return new WaitForSecondsRealtime(duration);

        float t = 0f;
        while (t < messageFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            messageText.alpha = 1f - Mathf.Clamp01(t / messageFadeDuration);
            yield return null;
        }

        messageText.alpha = 0f;
        messageRoutine = null;
    }

    public void ConnectPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("HUDController: el jugador es null.");
            return;
        }

        playerHealth = player.GetComponent<Health>();
        playerStats = player.GetComponent<PlayerStats>();

        if (playerHealth == null)
        {
            Debug.LogError("HUDController: Ike no tiene componente Health.");
            return;
        }

        // Escuchar todos los cambios de salud.
        playerHealth.OnChanged += HandleHealthChanged;

        // Mostrar la salud real inmediatamente.
        HandleHealthChanged(
            playerHealth.Current,
            playerHealth.maxHealth
        );

        if (playerStats != null)
            SetNutrition(playerStats.nutrition, playerStats.maxNutrition);
    }

    private void HandleHealthChanged(float current, float max)
    {
        SetHealth(current, max);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnChanged -= HandleHealthChanged;
    }

}
