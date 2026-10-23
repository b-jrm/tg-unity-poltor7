using UnityEngine;
public class Checkpoint : MonoBehaviour
{
    bool used;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;
        used = true;
        GameManager.I.SetCheckpoint(other.gameObject);
    }
}
