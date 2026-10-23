public interface IInteractable
{
    string Prompt { get; } // texto que se muestra: "[E] Prompt"
    void Interact(PlayerInventory who);
}