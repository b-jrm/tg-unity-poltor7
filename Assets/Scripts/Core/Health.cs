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

    //void Update()
    //{
    //    Debug.Log($"Health::Update() => {Current}");
    //}
    public void SetCurrent(float value)
    {
        //Debug.Log("SetCurrent(float value [" + value + "])");
        Current = Mathf.Clamp(value, 0f, maxHealth);
        OnChanged?.Invoke(Current, maxHealth);
    }
    public void TakeDamage(float amount)
    {
        //Debug.Log(
        //    $"[DAÑO] Objeto: {gameObject.name} | " +
        //    $"Daño recibido: {amount} | " +
        //    $"Salud antes: {Current}/{maxHealth}"
        //);

        if (IsDead || amount <= 0f)
            return;

        Current = Mathf.Max(0f, Current - amount);

        //Debug.Log(
        //    $"[DAÑO] Salud después: {Current}/{maxHealth}"
        //);

        OnChanged?.Invoke(Current, maxHealth);

        if (Current <= 0f)
            onDeath?.Invoke();
    }
    public void Heal(float amount)
    {
        //Debug.Log("Heal(float amount [" + amount + "])");
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        OnChanged?.Invoke(Current, maxHealth);
    }
}
