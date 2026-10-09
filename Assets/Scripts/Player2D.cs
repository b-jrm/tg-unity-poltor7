// POLTOR7 - Control 2D de Ike con animaciones.
// Guardar en: Assets/Scripts/Player2D.cs  (el nombre del archivo debe ser Player2D.cs)
// Requiere en el mismo objeto: Rigidbody2D, CapsuleCollider2D, Animator, SpriteRenderer
// Usa el Input System nuevo (Keyboard.current), igual que el resto del proyecto.
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class Player2D : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 3f;
    public float runSpeed = 5.5f;
    public float jumpForce = 10f;

    [Header("Suelo")]
    public LayerMask groundLayer;             // marcar la capa "Ground"
    public Vector2 groundCheckOffset = new Vector2(0f, 0.05f);
    public float groundCheckRadius = 0.15f;

    [Header("Combate (opcional)")]
    public float meleeRange = 0.9f;
    public float meleeDamage = 20f;
    public float shootDamage = 18f;
    public float shootRange = 8f;
    public LayerMask enemyLayer;              // capa "Enemy"

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    bool grounded;
    bool dead;
    float lockUntil;                          // bloquea moverse mientras ataca

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (dead) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        grounded = Physics2D.OverlapCircle((Vector2)transform.position + groundCheckOffset, groundCheckRadius, groundLayer);

        float h = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        bool running = kb.leftShiftKey.isPressed && h != 0f;

        bool busy = Time.time < lockUntil;
        float speed = running ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(busy ? 0f : h * speed, rb.linearVelocity.y);   // Unity 6. En Unity 2022 o anterior use rb.velocity

        if (h != 0f && !busy) sr.flipX = h < 0f;     // los sprites miran a la derecha

        // Saltar
        if (kb.spaceKey.wasPressedThisFrame && grounded && !busy)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.ResetTrigger("Jump");
            Trigger("Jump");
        }

        // Disparar (J) y golpear con la llave inglesa (K)
        if (kb.jKey.wasPressedThisFrame && !busy) { Trigger("Shoot"); lockUntil = Time.time + 0.3f; Shoot(); }
        if (kb.kKey.wasPressedThisFrame && !busy) { Trigger("Melee"); lockUntil = Time.time + 0.35f; Melee(); }

        // Parametros del Animator (bool en las animaciones en bucle)
        SetBool("Walk", h != 0f && !running && !busy);
        SetBool("Run", running && !busy);
    }

    void Melee()
    {
        Vector2 dir = sr.flipX ? Vector2.left : Vector2.right;
        Vector2 c = (Vector2)transform.position + Vector2.up * 0.5f + dir * (meleeRange * 0.5f);
        foreach (var col in Physics2D.OverlapCircleAll(c, meleeRange * 0.6f, enemyLayer))
        {
            var e = col.GetComponentInParent<Enemy2D>();
            if (e != null) e.TakeDamage(meleeDamage);
        }
    }

    void Shoot()
    {
        Vector2 dir = sr.flipX ? Vector2.left : Vector2.right;
        Vector2 o = (Vector2)transform.position + Vector2.up * 0.55f;
        RaycastHit2D hit = Physics2D.Raycast(o, dir, shootRange, enemyLayer);
        if (hit.collider != null)
        {
            var e = hit.collider.GetComponentInParent<Enemy2D>();
            if (e != null) e.TakeDamage(shootDamage);
        }
    }

    // Llamar desde enemigos o trampas
    public float health = 100f;
    public void TakeDamage(float dmg)
    {
        if (dead) return;
        health -= dmg;
        if (health <= 0f)
        {
            dead = true;
            rb.linearVelocity = Vector2.zero;
            Trigger("Die");
            return;
        }
        Trigger("Hurt");
    }

    // Helpers: no fallan si el Animator no tiene ese parametro
    void Trigger(string n) { foreach (var p in anim.parameters) if (p.name == n) { anim.SetTrigger(n); return; } }
    void SetBool(string n, bool v) { foreach (var p in anim.parameters) if (p.name == n) { anim.SetBool(n, v); return; } }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)transform.position + groundCheckOffset, groundCheckRadius);
    }
}
