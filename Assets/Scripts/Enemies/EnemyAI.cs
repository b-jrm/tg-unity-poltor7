
using UnityEngine;
using Gamekit2D;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Patrol, Chase, Attack, Dead }

    [Header("Movimiento")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public bool flying;

    [Tooltip("Sensor situado cerca de los pies del enemigo.")]
    public Transform groundSensor;

    [Tooltip("Sensor situado delante del enemigo.")]
    public Transform wallSensor;

    public LayerMask groundMask;
    [HideInInspector] public float speedMultiplier = 1f;

    [Header("Percepción")]
    public float sightRange = 10f;
    public float hearRange = 3f;
    public float loseSightTime = 4f;
    public LayerMask sightMask;

    [Header("Combate")]
    public float attackRange = 1.2f;
    public float damage = 10f;
    public float attackCooldown = 1.2f;

    [Range(0f, 1f)]
    public float accuracy = 1f;

    [Tooltip("Nombre del Trigger de ataque en el Animator.")]
    public string attackTrigger = "Attack";

    [Tooltip("Nombre del Trigger de daño recibido.")]
    public string hurtTrigger = "Hurt";

    [Tooltip("Nombre del Trigger de muerte.")]
    public string dieTrigger = "Die";

    [Tooltip("Tiempo entre la animación y la aplicación del daño.")]
    public float attackDamageDelay = 0.25f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float waitAtWaypoint = 1.5f;

    [Header("Botín")]
    public GameObject[] lootPrefabs;

    [Range(0f, 1f)]
    public float lootChance = 0.4f;

    [Header("Animación")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Tooltip("Parámetro Float opcional para la velocidad.")]
    public string speedParameter = "Speed";

    [Tooltip("Parámetro Bool opcional para caminar.")]
    public string walkParameter = "Walk";

    private Rigidbody2D rb;
    private Health health;
    private Transform player;

    private State state = State.Patrol;

    private int wp;
    private int dir = 1;

    private float wait;
    private float lastSeen;
    private float nextAttack;

    private bool attackPending;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.freezeRotation = true;

        health.onDeath.AddListener(Die);

        health.OnChanged += OnHealthChanged;

        initialized = true;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;

        if (waypoints != null && waypoints.Length > 0)
            wp = Mathf.Clamp(wp, 0, waypoints.Length - 1);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(Die);
            health.OnChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (!initialized || state == State.Dead)
            return;

        if (state == State.Patrol)
        {
            state = State.Chase;
            lastSeen = Time.time;
        }
    }

    private void Update()
    {
        if (state == State.Dead)
            return;

        if (player == null)
        {
            Stop();
            UpdateAnimations();
            return;
        }

        float dist = Vector2.Distance(
            transform.position,
            player.position
        );

        bool sees = CanSee(dist);

        if (sees)
            lastSeen = Time.time;

        switch (state)
        {
            case State.Patrol:
                if (sees)
                {
                    state = State.Chase;
                }
                else
                {
                    Patrol();
                }
                break;

            case State.Chase:
                if (sees && dist <= attackRange)
                {
                    state = State.Attack;
                    Stop();
                    Face(player.position);
                }
                else if (Time.time - lastSeen > loseSightTime)
                {
                    state = State.Patrol;
                    Stop();
                }
                else
                {
                    MoveToward(player.position, chaseSpeed);
                }
                break;

            case State.Attack:
                Stop();
                Face(player.position);

                if (!sees || dist > attackRange * 1.2f)
                {
                    CancelAttack();
                    state = State.Chase;
                    break;
                }

                if (Time.time >= nextAttack && !attackPending)
                    DoAttack();

                break;
        }

        UpdateAnimations();
    }

    private bool CanSee(float dist)
    {
        if (dist <= hearRange)
            return true;

        if (dist > sightRange)
            return false;

        float side = Mathf.Sign(
            player.position.x - transform.position.x
        );

        if (side != dir)
            return false;

        Vector2 origin = (Vector2)transform.position
            + Vector2.up * 0.5f;

        Vector2 target = (Vector2)player.position
            + Vector2.up * 0.5f;

        RaycastHit2D hit = Physics2D.Linecast(
            origin,
            target,
            sightMask
        );

        return hit.collider == null ||
               hit.collider.CompareTag("Player");
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Stop();
            return;
        }

        if (wp >= waypoints.Length || waypoints[wp] == null)
        {
            Stop();
            return;
        }

        Vector2 target = waypoints[wp].position;

        if (Mathf.Abs(target.x - transform.position.x) < 0.25f)
        {
            Stop();
            wait += Time.deltaTime;

            if (wait >= waitAtWaypoint)
            {
                wait = 0f;
                wp = (wp + 1) % waypoints.Length;
            }

            return;
        }

        MoveToward(target, patrolSpeed);
    }

    private void MoveToward(Vector2 target, float speed)
    {
        speed *= speedMultiplier;

        Vector2 delta = target - (Vector2)transform.position;

        if (Mathf.Abs(delta.x) < 0.01f)
        {
            Stop();
            return;
        }

        dir = delta.x >= 0f ? 1 : -1;
        Face(target);

        if (flying)
        {
            rb.linearVelocity = delta.normalized * speed;
            return;
        }

        // Un enemigo terrestre necesita detectar suelo.
        // Si falta el sensor, se detiene por seguridad.
        bool noGround = groundSensor == null ||
            !Physics2D.Raycast(
                groundSensor.position,
                Vector2.down,
                0.8f,
                groundMask
            );

        bool wall = wallSensor != null &&
            Physics2D.Raycast(
                wallSensor.position,
                Vector2.right * dir,
                0.3f,
                groundMask
            );

        // Evita avanzar por un borde o atravesar una pared.
        if (noGround || wall)
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
            return;
        }

        rb.linearVelocity = new Vector2(
            dir * speed,
            rb.linearVelocity.y
        );
    }

    private void Stop()
    {
        if (rb == null)
            return;

        rb.linearVelocity = flying
            ? Vector2.zero
            : new Vector2(0f, rb.linearVelocity.y);
    }

    private void Face(Vector2 target)
    {
        dir = target.x >= transform.position.x ? 1 : -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = dir < 0;
    }

    private void DoAttack()
    {
        nextAttack = Time.time + attackCooldown;
        attackPending = true;

        Trigger(attackTrigger);

        Invoke(nameof(ApplyAttackDamage), attackDamageDelay);
    }

    private void ApplyAttackDamage()
    {
        attackPending = false;

        if (state != State.Attack || player == null)
            return;

        float dist = Vector2.Distance(
            transform.position,
            player.position
        );

        if (dist > attackRange * 1.2f)
            return;

        if (Random.value > accuracy)
            return;

        PlayerController2D target =
            player.GetComponent<PlayerController2D>();

        if (target != null)
        {
            float multiplier = GameManager.I != null
                ? GameManager.I.damageTakenMultiplier
                : 1f;

            target.TakeDamage(damage * multiplier);
        }
    }

    private void CancelAttack()
    {
        CancelInvoke(nameof(ApplyAttackDamage));
        attackPending = false;
    }

    // Permite que Ike dañe al enemigo usando Health.
    // No se necesita EnemyController2D.
    public void TakeDamage(float dmg)
    {
        if (state == State.Dead || health == null)
            return;

        health.TakeDamage(dmg);

        // La muerte la gestiona el evento onDeath de Health.
        // No restar vida manualmente aquí.
    }

    private void Die()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;

        CancelAttack();
        Stop();

        if (animator != null)
            Trigger(dieTrigger);

        foreach (Collider2D col in
                 GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (lootPrefabs != null &&
            lootPrefabs.Length > 0 &&
            Random.value <= lootChance)
        {
            int index = Random.Range(0, lootPrefabs.Length);

            if (lootPrefabs[index] != null)
            {
                Instantiate(
                    lootPrefabs[index],
                    transform.position + Vector3.up * 0.3f,
                    Quaternion.identity
                );
            }
        }

        Destroy(gameObject, 6f);
    }

    private void UpdateAnimations()
    {
        if (animator == null || rb == null)
            return;

        SetFloat(
            speedParameter,
            rb.linearVelocity.magnitude
        );

        SetBool(
            walkParameter,
            state != State.Dead &&
            state != State.Attack &&
            Mathf.Abs(rb.linearVelocity.x) > 0.05f
        );
    }

    private void Trigger(string parameter)
    {
        if (animator == null ||
            string.IsNullOrEmpty(parameter))
            return;

        foreach (AnimatorControllerParameter p
                 in animator.parameters)
        {
            if (p.name == parameter &&
                p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameter);
                return;
            }
        }
    }

    private void SetFloat(string parameter, float value)
    {
        if (animator == null ||
            string.IsNullOrEmpty(parameter))
            return;

        foreach (AnimatorControllerParameter p
                 in animator.parameters)
        {
            if (p.name == parameter &&
                p.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameter, value);
                return;
            }
        }
    }

    private void SetBool(string parameter, bool value)
    {
        if (animator == null ||
            string.IsNullOrEmpty(parameter))
            return;

        foreach (AnimatorControllerParameter p
                 in animator.parameters)
        {
            if (p.name == parameter &&
                p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameter, value);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (groundSensor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(
                groundSensor.position,
                Vector3.down * 0.8f
            );
        }
    }
}
