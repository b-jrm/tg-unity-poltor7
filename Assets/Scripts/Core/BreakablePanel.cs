using UnityEngine;

public class BreakablePanel : MonoBehaviour // pared fragil
{
    public void Break() { Destroy(gameObject); }
}
