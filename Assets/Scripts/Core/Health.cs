using UnityEngine;
using UnityEngine.Events;
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public UnityEvent onDeath;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public event System.Action<float, float> OnChanged; // (actual, maximo)
    void Awake() { Current = maxHealth; }
    public void SetCurrent(float value)
    {
        Current = Mathf.Clamp(value, 0f, maxHealth);
        OnChanged?.Invoke(Current, maxHealth);
    }
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
        if (Current <= 0f) onDeath.Invoke();
    }
    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        OnChanged?.Invoke(Current, maxHealth);
    }
}
