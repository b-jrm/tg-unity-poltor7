
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundReference : MonoBehaviour
{
    [Range(0.05f, 1f)]
    public float opacity = 0.35f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyOpacity();
    }

    void OnValidate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyOpacity();
    }

    void ApplyOpacity()
    {
        if (spriteRenderer == null) return;

        Color color = spriteRenderer.color;
        color.a = opacity;
        spriteRenderer.color = color;
    }
}
