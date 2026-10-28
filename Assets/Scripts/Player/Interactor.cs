using UnityEngine;
using UnityEngine.InputSystem;
public class Interactor : MonoBehaviour
{
    public float radius = 1.5f;
    public LayerMask mask = ~0;
    PlayerInventory inv;
    IInteractable current;
    void Start() { inv = GetComponent<PlayerInventory>(); }
    void Update()
    {
        current = null;
        if (PlayerController.InputEnabled)
        {
            float best = float.MaxValue;
            foreach (var h in Physics2D.OverlapCircleAll(transform.position,
            radius, mask))
            {
                var it = h.GetComponentInParent<IInteractable>();
                if (it == null) continue;
                float d = ((Vector2)h.transform.position -
                (Vector2)transform.position).sqrMagnitude;
                // Debug.Log(d + " < " + best);
                if (d < best) { best = d; current = it; }
            }
        }
        // Debug.Log("current => " + current);
        HUDController.Prompt(current != null ? "[E] " + current.Prompt : "");
        if (current != null && Keyboard.current.eKey.wasPressedThisFrame)
            current.Interact(inv);
    }
}
