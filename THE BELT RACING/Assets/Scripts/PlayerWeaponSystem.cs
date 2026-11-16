
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponSystem : MonoBehaviour
{
    [Serializable]
    public class Weapon
    {
        [Header("Weapon Settings")]
        public string weaponName;
        public GameObject bulletPrefab;

        [Tooltip("All firing positions belonging to this weapon.")]
        public Transform[] firingPositions;

        [Tooltip("True = all positions fire together. False = fire sequentially.")]
        public bool fireAllPositions = true;

        public float bulletSpeed = 50f;
        public float fireRate = 0.25f;

        [Header("Debugging")]
        [HideInInspector] public int currentFiringPosition;

        [NonSerialized] public float nextFireTime;
    }

    [Header("Weapons")]
    [SerializeField] Weapon[] weapons;

    [Header("Debugging")]
    [SerializeField] bool isFiring;

    Rigidbody playerRigidbody;

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (isFiring)
        {
            TryFire();
        }
    }

    public void TryFire()
    {
        if (!isFiring)
            return;

        // Check every weapon independently.
        foreach (Weapon weapon in weapons)
        {
            TryFireWeapon(weapon);
        }
    }

    private void TryFireWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        if (Time.time < weapon.nextFireTime)
            return;

        if (weapon.bulletPrefab == null || weapon.firingPositions == null || weapon.firingPositions.Length == 0)
            return;

        // Set this weapon's next firing time.
        weapon.nextFireTime = Time.time + Mathf.Max(weapon.fireRate, 0.01f);

        if (weapon.fireAllPositions)
        {
            // Fire from every position simultaneously.
            foreach (Transform firingPosition in weapon.firingPositions)
            {
                FireBullet(weapon, firingPosition);
            }
        }
        else
        {
            // Fire from the current position.
            FireBullet(weapon, weapon.firingPositions[weapon.currentFiringPosition]);

            // Move to the next firing position.
            weapon.currentFiringPosition++;

            if (weapon.currentFiringPosition >= weapon.firingPositions.Length)
                weapon.currentFiringPosition = 0;
        }
    }

    private void FireBullet(Weapon weapon, Transform firingPosition)
    {
        if (firingPosition == null)
            return;

        // Instantiate bullet at the firing position.
        GameObject bullet = Instantiate(
            weapon.bulletPrefab,
            firingPosition.position,
            firingPosition.rotation
        );

        // Apply bullet velocity, including player momentum.
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            Vector3 playerVelocity = playerRigidbody != null
                ? playerRigidbody.linearVelocity
                : Vector3.zero;

            Vector3 firingVelocity = firingPosition.forward * weapon.bulletSpeed;

            bulletRb.linearVelocity = playerVelocity + firingVelocity;
        }
        else
        {
            Debug.LogWarning(
                "Bullet prefab is missing a Rigidbody!",
                bullet
            );
        }
    }

    #region Inputs

    public void OnFire(InputValue iv)
    {
        isFiring = iv.isPressed;

        // Fire immediately when the button is first pressed.
        if (isFiring)
        {
            TryFire();
        }
    }

    #endregion
}
