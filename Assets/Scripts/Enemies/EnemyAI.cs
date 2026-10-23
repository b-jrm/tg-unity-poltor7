using Gamekit2D;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    enum State { Patrol, Chase, Attack, Dead }
    [Header("Movimiento")]
    public float patrolSpeed = 1.5f, chaseSpeed = 3f;
    public bool flying; // Dron y Espora: Gravity Scale 0
    public Transform groundSensor, wallSensor; // hijos delante del enemigo
    public LayerMask groundMask;
    [HideInInspector] public float speedMultiplier = 1f;

    [Header("Percepcion")]
    public float sightRange = 10f, hearRange = 3f, loseSightTime = 4f;
    public LayerMask sightMask; // Ground + Player

    [Header("Combate")]
    public float attackRange = 1.2f; // Dron / Guardia: 8 a 10
    public float damage = 10f, attackCooldown = 1.2f;
    [Range(0f, 1f)] public float accuracy = 1f; // a distancia = 0.6

    [Header("Patrulla, botin y animacion")]
    public Transform[] waypoints;
    public float waitAtWaypoint = 1.5f;
    public GameObject[] lootPrefabs;
    [Range(0f, 1f)] public float lootChance = 0.4f;
    public Animator animator;
    Rigidbody2D rb; Health health; Transform player;
    State state = State.Patrol;
    int wp, dir = 1;
    float wait, lastSeen, nextAttack;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        health.onDeath.AddListener(Die);
        health.OnChanged += (cur, max) =>
        {
            if (state == State.Patrol)
            {
                state = State.Chase; lastSeen =
        Time.time;
            }
        };
    }
    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }
    void Update()
    {
        if (state == State.Dead || player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        bool sees = CanSee(dist);
        if (sees) lastSeen = Time.time;
        switch (state)
        {
            case State.Patrol:
                if (sees) state = State.Chase; else Patrol();
                break;
            case State.Chase:
                if (sees && dist <= attackRange) state = State.Attack;
                else if (Time.time - lastSeen > loseSightTime) state =
                State.Patrol;
                else MoveToward(player.position, chaseSpeed);
                break;
            case State.Attack:
                Stop();
                Face(player.position);
                if (!sees || dist > attackRange * 1.2f)
                {
                    state =
                State.Chase; break;
                }
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time +
                attackCooldown; DoAttack();
                }
                break;
        }
        if (animator != null) animator.SetFloat("Speed",
        rb.linearVelocity.magnitude);
    }
    bool CanSee(float dist)
    {
        if (dist <= hearRange) return true;
        if (dist > sightRange) return false;
        float side = Mathf.Sign(player.position.x - transform.position.x);
        if (side != dir) return false; // solo ve hacia delante
        var hit = Physics2D.Linecast(transform.position + Vector3.up * 0.5f,
        player.position + Vector3.up * 0.5f,
        sightMask);
        return hit.collider == null || hit.collider.CompareTag("Player");
    }
    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) { Stop(); return; }
        Vector2 target = waypoints[wp].position;
        if (Mathf.Abs(target.x - transform.position.x) < 0.25f)
        {
            Stop();
            wait += Time.deltaTime;
            if (wait >= waitAtWaypoint)
            {
                wait = 0f; wp = (wp + 1) %
            waypoints.Length;
            }
        }
        else MoveToward(target, patrolSpeed);
    }
    void MoveToward(Vector2 target, float speed)
    {
        speed *= speedMultiplier;
        Vector2 d = target - (Vector2)transform.position;
        dir = d.x >= 0f ? 1 : -1;
        transform.localScale = new Vector3(dir, 1f, 1f); // el sprite mira a la derecha
    if (flying) { rb.linearVelocity = d.normalized * speed; return; }
        bool noGround = groundSensor != null &&
        !Physics2D.Raycast(groundSensor.position, Vector2.down, 0.8f,
        groundMask);
        bool wall = wallSensor != null &&
        Physics2D.Raycast(wallSensor.position, Vector2.right * dir, 0.3f,
        groundMask);
        rb.linearVelocity = new Vector2((noGround || wall) ? 0f : dir *
        speed, rb.linearVelocity.y);
    }
    void Stop()
    {
        rb.linearVelocity = flying ? Vector2.zero : new Vector2(0f,
        rb.linearVelocity.y);
    }
    void Face(Vector2 p)
    {
        dir = p.x >= transform.position.x ? 1 : -1;
        transform.localScale = new Vector3(dir, 1f, 1f);
    }
    void DoAttack()
    {
        if (animator != null) animator.SetTrigger("Attack");
        if (Random.value > accuracy) return;
        var h = player.GetComponent<Health>();
        if (h != null) h.TakeDamage(damage *
        GameManager.I.damageTakenMultiplier);
    }
    void Die()
    {
        state = State.Dead;
        Stop();
        if (animator != null) animator.SetTrigger("Die");
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled =
        false;
        rb.simulated = flying ? false : rb.simulated;
        if (lootPrefabs.Length > 0 && Random.value <= lootChance)
            Instantiate(lootPrefabs[Random.Range(0, lootPrefabs.Length)],
            transform.position + Vector3.up * 0.3f,
            Quaternion.identity);
        Destroy(gameObject, 6f);
    }
}
