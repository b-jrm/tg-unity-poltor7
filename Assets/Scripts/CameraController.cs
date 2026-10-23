using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform aim;
    public float velocity = 0.025f;
    public Vector3 displacement;

    private void LateUpdate()
    {
        Vector3 position = aim.position + displacement;
        Vector3 positionSlow = Vector3.Lerp(transform.position, position, velocity);
        transform.position = positionSlow;
    }
}
