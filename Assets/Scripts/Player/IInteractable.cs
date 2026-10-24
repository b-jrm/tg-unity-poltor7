public interface IInteractable
{
    string Prompt { get; } // Texto que se muestra: "[E] Prompt"
    void Interact(PlayerInventory who);
}