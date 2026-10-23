using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static bool InputEnabled = true; // lo consultan los demas scripts
    
    [Header("Movimiento")]
    public float walkSpeed = 4f;
    public float runSpeed = 6.5f;
    public float jumpHeight = 2.4f; // Nivel 4: 4
    public float coyoteTime = 0.1f, jumpBuffer = 0.1f;
    
    [Header("Suelo")]
    public Transform groundCheck;
    public float groundRadius = 0.15f;
    public LayerMask groundMask;

    [Header("Apuntado")]
    public Transform armPivot; // el arma rota aqui
    public SpriteRenderer body; // sprite que mira a la derecha
    public Camera cam;
    public Animator animator;
    public Vector2 LastSafe { get; private set; }
    Rigidbody2D rb;
    float moveX, coyote, buffer;
    bool grounded, jumpHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        InputEnabled = true;
        if (cam == null) cam = Camera.main;
        LastSafe = transform.position;
    }

    void Update()
    {
        grounded = Physics2D.OverlapCircle(groundCheck.position,
        groundRadius, groundMask);
        coyote = grounded ? coyoteTime : coyote - Time.deltaTime;
        if (grounded) LastSafe = transform.position;
        moveX = 0f;
        if (!InputEnabled) { buffer = 0f; return; }
        var kb = Keyboard.current;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
        moveX *= kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        if (kb.spaceKey.wasPressedThisFrame) buffer = jumpBuffer;
        else buffer
        -= Time.deltaTime;
        jumpHeld = kb.spaceKey.isPressed;
        Aim();
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveX));
            animator.SetBool("Grounded", grounded);
        }
    }

    void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        v.x = moveX;
        if (buffer > 0f && coyote > 0f)
        {
            float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            v.y = Mathf.Sqrt(2f * g * jumpHeight);
            buffer = 0f; coyote = 0f;
        }
        if (v.y > 0f && !jumpHeld) v.y *= 0.92f; // salto variable
        rb.linearVelocity = v;
    }
    
    void Aim()
    {
        Vector3 m =
        cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = (Vector2)m - (Vector2)armPivot.position;
        bool right = dir.x >= 0f;
        body.flipX = !right;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        armPivot.rotation = Quaternion.Euler(0f, 0f, ang);
        armPivot.localScale = new Vector3(1f, right ? 1f : -1f, 1f); // evita el arma al reves
    }

}