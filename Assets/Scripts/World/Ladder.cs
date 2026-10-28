
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Ladder : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerController2D playerController;

    [Header("Ladder")]
    public float climbSpeed = 3f;
    public float exitHop = 3f;
    public float normalGravity = 3.5f;

    [HideInInspector]
    public bool onLadder = false;

    private float verticalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController2D>();
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (kb == null)
        {
            verticalInput = 0f;
            return;
        }

        verticalInput = 0f;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            verticalInput = 1f;

        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            verticalInput = -1f;
    }

    void FixedUpdate()
    {
        if (!onLadder)
            return;

        // Subir o bajar sin multiplicar por deltaTime.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            verticalInput * climbSpeed
        );

        //if (animator != null)
        //{
        //    animator.SetBool("onLadder", true);
        //    animator.SetFloat("vSpeed", Mathf.Abs(verticalInput));
        //}
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Debug.Log("Ike detecta => " + collision.name);

        if (!collision.CompareTag("Ladder"))
            return;

        // Debug.Log("Mathf.Abs(verticalInput) => " + Mathf.Abs(verticalInput));
        // Debug.Log("onLadder => " + onLadder);
        // Activar la escalada al pulsar arriba o abajo.
        if (!onLadder && Mathf.Abs(verticalInput) > 0)
        {
            onLadder = true;
            rb.gravityScale = 0f;

            if (playerController != null)
                playerController.usingLadder = true;

            //if (animator != null)
            //    animator.SetBool("onLadder", true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Ladder") || !onLadder)
            return;

        onLadder = false;
        rb.gravityScale = normalGravity;

        if (playerController != null)
            playerController.usingLadder = false;

        //if (animator != null)
        //{
        //    animator.SetBool("onLadder", false);
        //    animator.SetFloat("vSpeed", 0f);
        //}

        // Pequeño impulso al salir si Ike está en el aire.
        if (playerController != null && !playerController.grounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                exitHop
            );
        }
    }
}
