using UnityEngine;
public class LevelExit : MonoBehaviour
{
    public string nextScene = "DeadGarden"; // Level 2
    public string requiredFlag; // ej. "boss1_dead"
    public string requiredItem; // ej. "card_blue"
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!string.IsNullOrEmpty(requiredFlag) &&
        !GameManager.I.HasFlag(requiredFlag))
        { HUDController.Msg("The elevator has no power."); return; }
        var inv = other.GetComponent<PlayerInventory>();
        if (!string.IsNullOrEmpty(requiredItem) && !inv.Has(requiredItem))
        { HUDController.Msg("You need an access card."); return; }
        GameManager.I.CompleteLevel(other.gameObject, nextScene);
    }
}