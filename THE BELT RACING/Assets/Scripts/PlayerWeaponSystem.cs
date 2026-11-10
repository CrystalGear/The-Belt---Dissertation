
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform[] firingPositions;

    [Tooltip("True = all firing positions fire together. False = fire sequentially.")]
    [SerializeField] bool fireAllPositions = true;

    [SerializeField] float bulletSpeed = 50f;
    [SerializeField] float fireRate = 0.25f;

    [Header("Velocity Inheritance")]
    [SerializeField] bool inheritPlayerVelocity = true;

    [Header("Input")]
    [SerializeField] bool automaticFire = true;

    [Header("Debugging")]
    [SerializeField] bool isFiring;

    Rigidbody playerRigidbody;

    float nextFireTime;
    int currentFiringPosition;

    void Start()
    {
        // Get the Rigidbody from the player.
        playerRigidbody = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (automaticFire && isFiring)
        {
            TryFire();
        }
    }

    public void TryFire()
    {
        if (Time.time < nextFireTime)
            return;

        if (bulletPrefab == null || firingPositions.Length == 0)
            return;

        nextFireTime = Time.time + fireRate;

        if (fireAllPositions)
        {
            // Fire from every position simultaneously.
            foreach (Transform firingPosition in firingPositions)
            {
                FireBullet(firingPosition);
            }
        }
        else
        {
            // Fire from the current position.
            FireBullet(firingPositions[currentFiringPosition]);

            // Move to the next firing position.
            currentFiringPosition++;

            if (currentFiringPosition >= firingPositions.Length)
                currentFiringPosition = 0;
        }
    }

    void FireBullet(Transform firingPosition)
    {
        if (firingPosition == null)
            return;

        // Instantiate the bullet facing the firing position's forward direction.
        GameObject bullet = Instantiate(
            bulletPrefab,
            firingPosition.position,
            firingPosition.rotation
        );

        // Apply forward velocity using physics.
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            Vector3 bulletVelocity = firingPosition.forward * bulletSpeed;

            // Inherit the player's current velocity.
            if (inheritPlayerVelocity && playerRigidbody != null)
            {
                bulletVelocity += playerRigidbody.linearVelocity;
            }

            bulletRb.linearVelocity = bulletVelocity;
        }
        else
        {
            Debug.LogWarning("Bullet prefab is missing a Rigidbody!", bullet);
        }
    }

    #region Inputs

    public void OnFire(InputValue iv)
    {
        isFiring = iv.isPressed;
    }

    #endregion
}
