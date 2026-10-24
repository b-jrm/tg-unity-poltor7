using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform aim;
    public float smoothSpeed = 5f;

    [Header("Camera Offset")]
    public Vector3 displacement = new Vector3(0f, 1.5f, -10f);

    private void LateUpdate()
    {
        if (aim == null) return;

        Vector3 targetPosition = aim.position + displacement;

        // Seguimiento suave independiente de la tasa de fotogramas
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
