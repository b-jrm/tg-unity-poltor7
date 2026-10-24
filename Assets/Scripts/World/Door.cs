using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class Door : MonoBehaviour, IInteractable
{
    public bool locked;
    public string requiredKeyId; // vacio = no usa tarjeta (lo abre un puzle)
    public Vector3 openOffset = new Vector3(3f, 0f, 0f);
    public float speed = 4f;
    public UnityEvent onOpened;
    Vector3 closedPos, openPos;
    bool isOpen;
    Coroutine routine;
    public string Prompt => locked
    ? (string.IsNullOrEmpty(requiredKeyId) ? "Bloqueada" : "Necesita tarjeta")
    : (isOpen ? "Cerrar" : "Abrir");
    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + openOffset;
    }
    public void Interact(PlayerInventory inv)
    {
        Debug.Log("Ike interactuó con la puerta");
        if (locked)
        {
            if (!string.IsNullOrEmpty(requiredKeyId) && inv.Has(requiredKeyId))
            {
                locked = false;
                HUDController.Msg("Acceso concedido");
            }
            else
            {
                HUDController.Msg("La puerta esta bloqueada.");
                return;
            }
        }
        Toggle();
    }
    public void Unlock() { locked = false; } // para conectar con onSolved de un puzle
    public void Toggle()
    {
        isOpen = !isOpen;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(MoveTo(isOpen ? openPos : closedPos));
        if (isOpen) onOpened.Invoke();
    }
    IEnumerator MoveTo(Vector3 target)
    {
        while ((transform.position - target).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}