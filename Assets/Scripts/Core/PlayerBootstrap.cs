// ---------- PlayerBootstrap.cs (en el Player) ----------
using UnityEngine;
public class PlayerBootstrap : MonoBehaviour
{
    void Start() { GameManager.I.Restore(gameObject); }
}
