
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] float damage = 10f;

    [Header("Movement Settings")]
    [SerializeField] float projectileSpeed = 100f;
    [SerializeField] float lifetime = 10f;

    [Header("Homing Settings")]
    [SerializeField] bool useHoming = false;
    [SerializeField] float turnSpeed = 180f;

    Rigidbody rb;
    Transform target;
    bool hasImpacted;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Automatically find the player.
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }

        // Set initial projectile velocity.
        rb.linearVelocity = transform.forward * projectileSpeed;

        // Destroy projectile after its lifetime.
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (!useHoming || target == null || hasImpacted)
            return;

        HomeTowardsTarget();
    }

    void HomeTowardsTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        rb.linearVelocity = transform.forward * projectileSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted)
            return;

        HandleImpact(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasImpacted)
            return;

        HandleImpact(other);
    }

    void HandleImpact(Collider other)
    {
        hasImpacted = true;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
