// POLTOR7 - IA 2D simple para enemigos con animaciones.
// Guardar en: Assets/Scripts/Enemy2D.cs
// Requiere: Rigidbody2D, Collider2D (CapsuleCollider2D), Animator, SpriteRenderer
// Sirve para Raizal, Chatarrero, Guardia, Chatarrero Alfa, Vex, Dron y Espora
// (cambie "Move Param" a "Move" en Dron y Espora; en el Dron marque "Flying").
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class Enemy2D : MonoBehaviour
{
    [Header("Vida y daño")]
    public float health = 50f;
    public float damage = 8f;
    public float attackCooldown = 1f;

    [Header("Movimiento")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.2f;
    public float patrolDistance = 3f;         // recorre +/- esta distancia desde donde empieza
    public bool flying;                        // true en el Dron (no cae, flota)

    [Header("Percepcion")]
    public float sightRange = 6f;
    public float attackRange = 0.9f;           // Dron/Guardia/Vex: 5 a 6 (atacan a distancia)

    [Header("Animator")]
    public string moveParam = "Walk";          // "Walk" o "Move" (Dron, Espora)
    public string attackTrigger = "Attack";    // "Shoot" en Guardia, Dron y Vex; "Explode" en Espora
    public bool explodeOnAttack;               // true en la Espora: se destruye al explotar

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Transform player;
    Vector2 startPos;
    int dir = 1;
    float nextAttack;
    bool dead;
    float stunUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
        if (flying) rb.gravityScale = 0f;
        startPos = transform.position;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (dead) return;
        if (Time.time < stunUntil) { Stop(); return; }

        float dist = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        if (dist <= attackRange)
        {
            Stop();
            Face(player.position.x - transform.position.x);
            if (Time.time >= nextAttack) Attack();
        }
        else if (dist <= sightRange)
        {
            float dx = player.position.x - transform.position.x;
            Face(dx);
            Move(Mathf.Sign(dx) * chaseSpeed);
        }
        else
        {
            // Patrulla de ida y vuelta
            if (transform.position.x > startPos.x + patrolDistance) dir = -1;
            if (transform.position.x < startPos.x - patrolDistance) dir = 1;
            Face(dir);
            Move(dir * patrolSpeed);
        }
    }

    void Move(float vx)
    {
        rb.linearVelocity = new Vector2(vx, flying ? 0f : rb.linearVelocity.y);   // Unity 6. En 2022 o anterior: rb.velocity
        SetBool(moveParam, true);
    }

    void Stop()
    {
        rb.linearVelocity = new Vector2(0f, flying ? 0f : rb.linearVelocity.y);
        SetBool(moveParam, false);
    }

    void Face(float dx)
    {
        if (dx > 0.01f) sr.flipX = false;
        else if (dx < -0.01f) sr.flipX = true;
    }

    void Attack()
    {
        nextAttack = Time.time + attackCooldown;
        Trigger(attackTrigger);
        StartCoroutine(DealDamage(0.25f));   // el daño llega a mitad de la animacion
    }

    IEnumerator DealDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dead || player == null) yield break;
        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.4f)
        {
            var p = player.GetComponent<Player2D>();
            if (p != null) p.TakeDamage(damage);
        }
        if (explodeOnAttack)
        {
            dead = true;
            Stop();
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, 0.8f);
        }
    }

    public void TakeDamage(float dmg)
    {
        if (dead) return;
        health -= dmg;
        if (health <= 0f)
        {
            dead = true;
            Stop();
            rb.simulated = flying ? true : rb.simulated;
            if (flying) rb.gravityScale = 2f;            // el dron cae al morir
            GetComponent<Collider2D>().enabled = flying;
            Trigger("Die");
            Destroy(gameObject, 4f);
            return;
        }
        stunUntil = Time.time + 0.3f;
        Trigger("Hurt");
    }

    void Trigger(string n) { foreach (var p in anim.parameters) if (p.name == n) { anim.SetTrigger(n); return; } }
    void SetBool(string n, bool v) { foreach (var p in anim.parameters) if (p.name == n) { anim.SetBool(n, v); return; } }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
