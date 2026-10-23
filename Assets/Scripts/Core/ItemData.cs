using UnityEngine;
public enum ItemType { Key, Weapon, Health, Food, Ammo, Utility, Quest }
[CreateAssetMenu(menuName = "Poltor7/Item")]
public class ItemData : ScriptableObject
{
    public string id; // ejemplo: "medkit", "card_blue", "ammo_p7"
    public string displayName;
    [TextArea] public string description;
    public ItemType type;
    public Sprite icon;
    public int maxStack = 99; // botiquin = 5, racion = 6
}