using JetBrains.Annotations;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    Rigidbody rb;

    [Header("Movement")]
    public float speed = 6f;

    [Header("Health")]
    public int maxHealth = 200;
    int currentHealth;

    [Header("Sword")]
    public Collider bladeCollider;

    [Header("Jumping")]
    public float jumpHeight = 1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.1f;
    public LayerMask groundMask;

    public Animator animator;

    bool isGrounded;
    float horiz, vert;
    bool jumpRequest;
    bool attackRequest;
    bool isAttacking;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // Read inputs once per frame
        horiz = Input.GetAxis("Horizontal");
        vert  = Input.GetAxis("Vertical");

        if (Input.GetButtonDown("Jump") && isGrounded)
            jumpRequest = true;

        if (Input.GetButtonDown("Fire1") && !isAttacking)
        {
            attackRequest = true;
        }
    }

    void FixedUpdate()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("Horizontal", horiz, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical",   vert,  0.1f, Time.deltaTime);
        animator.SetBool("isAttacking", isAttacking);

        if (attackRequest)
        {   
            isAttacking = true;
            EnableSword();
            animator.SetBool("isAttacking", true);
            animator.SetTrigger("Attack");
            attackRequest = false;
        }

        Vector3 move = transform.right * horiz + transform.forward * vert;
        Vector3 targetPos = rb.position + speed * Time.fixedDeltaTime * move;
        rb.MovePosition(targetPos);

        if (jumpRequest)
        {
            // v = sqrt(2 * g * h)
            float jumpVel = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            Vector3 vel = rb.linearVelocity;
            vel.y = jumpVel;
            rb.linearVelocity = vel;
            animator.SetTrigger("Jump");
            jumpRequest = false;
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage, HP = {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Disable player control
        GetComponent<PlayerController>().enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIManager.Instance.ShowStartScreen();
        Generator.Instance.ResetGame();
        Destroy(gameObject);
    }

    public void OnAttackComplete()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);
        animator.ResetTrigger("Attack");
        DisableSword();
    }

    public void EnableSword()  
    {
        Debug.Log("EnableSword called");
        if (bladeCollider != null) bladeCollider.enabled = true; 
    }

    public void DisableSword()  
    { 
        Debug.Log("DisableSword called");
        if (bladeCollider != null) bladeCollider.enabled = false; 
    }
}
