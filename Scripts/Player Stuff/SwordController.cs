using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SwordController : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 25;

    Collider swordCollider;
    Rigidbody swordRb;

    void Awake()
    {
        swordCollider = GetComponent<Collider>();
        swordRb = GetComponent<Rigidbody>();

        swordCollider.isTrigger = true;
        swordCollider.enabled = false;

        swordRb.isKinematic = true;
        swordRb.useGravity = false;
    }

    // Called by Animation Events on your Attack clip
    public void EnableBlade()  => GetComponent<Collider>().enabled = true;
    public void DisableBlade() => GetComponent<Collider>().enabled = false;

    // Overlap check instead of collision check
    void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            Debug.Log("Enemy hit: " + other.name);
            enemy.takeDamage(damage);
        }
    }
}
