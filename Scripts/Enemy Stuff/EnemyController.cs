using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    int   currentHealth;

    [Header("Attack Settings")]
    public int damage = 10;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.4f;
    float nextAttackTime = 0f;

    [Header("Detection")]
    public float detectionRadius = 25f;
    [Range(0, 360)]
    public float viewAngle = 120f;

    NavMeshAgent agent;
    Transform player;
    public Animator animator;

    void Awake()
    {
        currentHealth = maxHealth;
        agent  = GetComponent<NavMeshAgent>();
        // Let the agent rotate itself toward the movement direction:
        agent.updateRotation = true;
        agent.stoppingDistance = attackRange;

        var pgo = GameObject.FindWithTag("Player");
        if (pgo != null)
            player = pgo.transform;
        else
            Debug.LogError("EnemyController: no GameObject tagged 'Player' in scene!");
    }

    void Update() 
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inAttackWindow = Time.time < nextAttackTime && dist <= attackRange;

        // freeze when attacking
        agent.isStopped = inAttackWindow;
        if (ifNearPlayer() && !inAttackWindow)
        {
            // very close: attack window logic
            agent.SetDestination(player.position);

            if (Time.time >= nextAttackTime && dist <= attackRange)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        else if (!inAttackWindow && CanSeePlayer())
        {
            // saw them at range: chase
            agent.SetDestination(player.position);

            if (Time.time >= nextAttackTime && dist <= attackRange)
            {
                Attack();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        else if (!inAttackWindow)
        {
            // neither near nor seen: stop
            agent.ResetPath();
        }

        if (!inAttackWindow) {
            Vector3 localVel = transform.InverseTransformDirection(agent.velocity);
            animator.SetFloat("Horizontal", localVel.x / agent.speed, 0.1f, Time.deltaTime);
            animator.SetFloat("Vertical",   localVel.z / agent.speed, 0.1f, Time.deltaTime);
        } else {
            animator.SetFloat("Horizontal", 0, 0.1f, Time.deltaTime);
            animator.SetFloat("Vertical",   0, 0.1f, Time.deltaTime);
        }
    }

    bool ifNearPlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > detectionRadius && dist <= attackRange * 1.2f)
        {
           return true;
        } else {
            return false;
        }
        
    }

    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;
        if (dist > detectionRadius)
            return false;

        if (dist <= attackRange * 1.2f) 
            return true;

        // angle test
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle * 0.5f) return false;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        RaycastHit[] hits = Physics.RaycastAll(origin, toPlayer.normalized, dist);
        foreach (var h in hits) {
            if (h.collider.CompareTag("Player")) 
                return true;
        }
        return false;
    }

    void Attack()
    {
        animator.SetTrigger("Attack");

        var currentPlayer = player.GetComponent<PlayerController>();
        if (currentPlayer)
        {
            currentPlayer.TakeDamage(damage);
        }
    }

    public void takeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
