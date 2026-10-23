using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Health), typeof(PlayerInventory))]
public class PlayerStats : MonoBehaviour
{
    public float nutrition = 100f;
    public float maxNutrition = 100f;
    public float decayPerSecond = 0.15f;
    public float starveDamagePerSecond = 1f;
    Health hp;
    PlayerInventory inv;
    float poisonTime, poisonDps;
    void Awake()
    {
        hp = GetComponent<Health>();
        inv = GetComponent<PlayerInventory>();
    }
    void Update()
    {
        nutrition = Mathf.Max(0f, nutrition - decayPerSecond * Time.deltaTime);
        if (nutrition <= 0f) hp.TakeDamage(starveDamagePerSecond * Time.deltaTime);
        if (poisonTime > 0f)
        {
            poisonTime -= Time.deltaTime;
            hp.TakeDamage(poisonDps * Time.deltaTime);
        }
        if (!PlayerController.InputEnabled) return;
        var kb = Keyboard.current;
        if (kb.hKey.wasPressedThisFrame && hp.Current < hp.maxHealth && inv.Remove("medkit"))
            hp.Heal(40f);
        if (kb.fKey.wasPressedThisFrame && nutrition < maxNutrition && inv.Remove("ration"))
            nutrition = Mathf.Min(maxNutrition, nutrition + 30f);
        if (kb.gKey.wasPressedThisFrame && poisonTime > 0f && inv.Remove("antidote"))
            poisonTime = 0f;
    }
    // Lo llaman las esporas: Poison(10f, 3f) = 3 de dano por segundo durante 10 s
    public void Poison(float seconds, float dps) { poisonTime = seconds; poisonDps = dps; }
}