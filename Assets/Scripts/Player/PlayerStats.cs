
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health), typeof(PlayerInventory))]
public class PlayerStats : MonoBehaviour
{
    public float nutrition = 100f;
    public float maxNutrition = 100f;
    public float decayPerSecond = 0.15f;
    public float starveDamagePerSecond = 1f;

    private Health hp;
    private PlayerInventory inv;

    private float poisonTime;
    private float poisonDps;

    private void Awake()
    {
        hp = GetComponent<Health>();
        inv = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        if (HUDController.Instance != null)
            HUDController.Instance.ConnectPlayer(gameObject);

        UpdateHUD();
    }

    private void Update()
    {
        nutrition = Mathf.Max(
            0f,
            nutrition - decayPerSecond * Time.deltaTime
        );

        if (nutrition <= 0f)
            hp.TakeDamage(starveDamagePerSecond * Time.deltaTime);

        if (poisonTime > 0f)
        {
            poisonTime -= Time.deltaTime;
            hp.TakeDamage(poisonDps * Time.deltaTime);
        }

        if (!PlayerController.InputEnabled)
            return;

        if (Keyboard.current == null)
            return;

        var kb = Keyboard.current;

        // Usar un botiquín con H.
        if (kb.hKey.wasPressedThisFrame)
        {
            Debug.Log(
                $"ANTES: Salud={hp.Current}/{hp.maxHealth}, " +
                $"Botiquines={inv.Count("medkit")}"
            );

            if (hp.Current < hp.maxHealth && inv.Remove("medkit"))
            {
                hp.Heal(40f);

                Debug.Log(
                    $"DESPUÉS: Salud={hp.Current}/{hp.maxHealth}, " +
                    $"Botiquines={inv.Count("medkit")}"
                );

                HUDController.Msg("Botiquín usado: +40 de salud");
            }
            else
            {
                Debug.Log("No se usó el botiquín: salud máxima o inventario vacío.");
            }
        }

        // Consumir alimento con F.
        if (kb.fKey.wasPressedThisFrame)
        {

            Debug.Log(
                $"ANTES: Alimento={hp.Current}/{hp.maxHealth}, " +
                $"Alimentos={inv.Count("ration")}"
            );

            if (nutrition < maxNutrition && inv.Remove("ration"))
            {
                nutrition = Mathf.Min(maxNutrition, nutrition + 30f);

                Debug.Log(
                    $"DESPUÉS: Alimento={nutrition}/{maxNutrition}, " +
                    $"Alimentos={inv.Count("ration")}"
                );

                HUDController.Msg("Alimento usado: +30");
            }
            else
            {
                Debug.Log("No se usó el Alimento: alimento máximo o inventario vacío.");
            }

            
        }

        // Usar antídoto con G.
        if (kb.gKey.wasPressedThisFrame &&
            poisonTime > 0f &&
            inv.Remove("antidote"))
        {
            poisonTime = 0f;
            poisonDps = 0f;
            HUDController.Msg("Veneno eliminado");
        }

        UpdateHUD();
    }

    private void UpdateHUD()
    {
        // Debug.Log("PalyerStats::UpdateHUD() => " + HUDController.Instance);
        // Debug.Log($"PalyerStats::hp => {hp}");

        if (HUDController.Instance == null)
            return;

        HUDController.Instance.SetHealth(hp.Current, hp.maxHealth);
        HUDController.Instance.SetNutrition(nutrition, maxNutrition);
        HUDController.Instance.SetMedkits(inv.Count("medkit"));
    }

    public void Poison(float seconds, float dps)
    {
        poisonTime = seconds;
        poisonDps = dps;
    }
}
