using UnityEngine;
public class VoidZone : MonoBehaviour
{
    public float damage = 20f;
    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.CompareTag("Player")) return;
        var pc = o.GetComponent<PlayerController>();
        o.GetComponent<Health>().TakeDamage(damage);
        o.transform.position = pc.LastSafe;
        if (o.attachedRigidbody != null) o.attachedRigidbody.linearVelocity =
        Vector2.zero;
    }
}
