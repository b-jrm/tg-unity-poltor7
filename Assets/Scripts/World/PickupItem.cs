using NUnit;
using UnityEngine;
public class PickupItem : MonoBehaviour, IInteractable
{
    public ItemData item;
    public int amount = 1;
    public string uniqueId; // opcional: evita que reaparezca tras cargar partida
    public AudioClip sfx;
    public string Prompt => "Recoger " + item.displayName;
    void Start()
    {
        Debug.Log($"PickupItem::Start() => uniqueId {uniqueId}");
        if (!string.IsNullOrEmpty(uniqueId) && GameManager.I.HasFlag("pk_" + uniqueId))
            Destroy(gameObject);
    }
    public void Interact(PlayerInventory inv)
    {
        Debug.Log($"PickupItem::Interact(PlayerInventory inv: ${inv})");
        if (!inv.Add(item, amount))
        {
            HUDController.Msg("No puedes llevar mas " + item.displayName);
            return;
        }
        if (!string.IsNullOrEmpty(uniqueId)) GameManager.I.SetFlag("pk_" + uniqueId);
        HUDController.Msg("Obtenido: " + item.displayName + " x" + amount);
        if (sfx != null) AudioSource.PlayClipAtPoint(sfx, transform.position);
        Destroy(gameObject);
    }
}
