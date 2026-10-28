using UnityEngine;
using UnityEngine.InputSystem;
public class WeaponManager : MonoBehaviour
{
    public WeaponHitscan[] weapons; // orden: 1 llave, 2 P-7, 3 escopeta, 4 rifle
    public WeaponHitscan Current => current >= 0 ? weapons[current] : null;
    PlayerInventory inv;
    int current = -1;
    void Start()
    {
        inv = GetComponentInParent<PlayerInventory>();
        Select(-1);
    }
    bool Owns(int i) =>
    string.IsNullOrEmpty(weapons[i].weaponItemId) || inv.Has(weapons[i].weaponItemId);
    void Update()
    {
        if (!PlayerController.InputEnabled) return;
        if (current == -1) // armar automaticamente el primer arma disponible
            for (int i = 0; i < weapons.Length; i++)
                if (Owns(i)) { Select(i); break; }
        var kb = Keyboard.current;
        for (int i = 0; i < weapons.Length && i < 9; i++)
        {
            var key = kb[(Key)((int)Key.Digit1 + i)];
            if (key.wasPressedThisFrame && Owns(i)) Select(i);
        }
    }
    void Select(int index)
    {
        current = index;
        for (int i = 0; i < weapons.Length; i++)
            weapons[i].gameObject.SetActive(i == index);
    }
}
