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
        if (Count(data.id) >= data.maxStack) return false;
        items[data.id] = Mathf.Min(data.maxStack, Count(data.id) + n);
        KeyItemsHUD.Instance?.ShowKeyItem(data);
        OnChanged?.Invoke();
        return true;
    }
    public void Add(string id, int n = 1)
    {
        items[id] = Count(id) + n;
        OnChanged?.Invoke();
    }
    public bool Remove(string id, int n = 1)
    {
        //Debug.Log($"Remove(id: {id}, int n: {n})");
        if (!Has(id, n)) return false;
        items[id] -= n;
        //Debug.Log($"items[{id}]: {items[id]}");
        if (items[id] <= 0) items.Remove(id);
        OnChanged?.Invoke();
        return true;
    }
    public List<ItemEntry> Export()
    {
        var list = new List<ItemEntry>();
        foreach (var kv in items) list.Add(new ItemEntry { id = kv.Key, amount = kv.Value });
        return list;
    }
    public void Import(List<ItemEntry> list)
    {
        items.Clear();
        foreach (var e in list) items[e.id] = e.amount;
        OnChanged?.Invoke();
    }
}
