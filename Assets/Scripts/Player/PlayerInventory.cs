using NUnit;
using System.Collections.Generic;
using UnityEngine;
public class PlayerInventory : MonoBehaviour
{
    readonly Dictionary<string, int> items = new Dictionary<string, int>();
    public event System.Action OnChanged;
    public int Count(string id) => items.TryGetValue(id, out var n) ? n : 0;
    public bool Has(string id, int n = 1) => Count(id) >= n;
    // Devuelve false si el inventario de ese objeto esta lleno.
    public bool Add(ItemData data, int n = 1)
    {
        Debug.Log($"PlayerInventory::Add1() => data: {data}");
        if (Count(data.id) >= data.maxStack) return false;
        items[data.id] = Mathf.Min(data.maxStack, Count(data.id) + n);
        Debug.Log($"PlayerInventory::Add1() => items: {items}");
        KeyItemsHUD.Instance?.ShowKeyItem(data);
        OnChanged?.Invoke();
        return true;
    }
    public void Add(string id, int n = 1)
    {
        Debug.Log($"PlayerInventory::Add2() => items: {items}");
        items[id] = Count(id) + n;
        OnChanged?.Invoke();
    }
    public bool Remove(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0)
            return false;

        int current = Count(id);

        if (current < amount)
            return false;

        int remaining = current - amount;

        if (remaining <= 0) 
            items.Remove(id);
        else
            items[id] = remaining;

        OnChanged?.Invoke();

        if (Count(id) <= 0)
        {
            KeyItemsHUD.Instance?.RemoveKeyItem(id);
        }

        Debug.Log($"Inventario: {id} restante = {Count(id)}");

        return true;
    }
    public List<ItemEntry> Export()
    {
        var list = new List<ItemEntry>();
        foreach (var kv in items) list.Add(new ItemEntry { id = kv.Key, amount = kv.Value });
        Debug.Log($"inventory: {list}");
        return list;
    }
    public void Import(List<ItemEntry> list)
    {
        items.Clear();
        foreach (var e in list) items[e.id] = e.amount;
        OnChanged?.Invoke();
    }
}
