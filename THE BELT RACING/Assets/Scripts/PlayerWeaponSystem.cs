
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

    [Header("Aiming")]
    [Tooltip("The UI flight reticle used to determine the aiming direction.")]
    [SerializeField] RectTransform flightReticle;

    [Tooltip("The camera used to cast the aiming ray.")]
    [SerializeField] Camera flightCamera;

    [Tooltip("Objects that can be targeted by the aiming ray.")]
    [SerializeField] LayerMask aimLayers = ~0;

    [Tooltip("Maximum distance used when nothing is hit.")]
    [SerializeField] float aimRayDistance = 2000f;

    [Header("Debugging")]
    [SerializeField] bool isFiring;
    [SerializeField] Vector3 currentAimPoint;

    Rigidbody playerRigidbody;

    // Reusable raycast results to avoid allocating an array every frame.
    readonly RaycastHit[] aimHits = new RaycastHit[32];

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (flightCamera == null)
            flightCamera = Camera.main;

        UpdateAimPoint();
    }

    private void Update()
    {
        UpdateAimPoint();

        if (isFiring)
        {
            TryFire();
        }
    }

    private void UpdateAimPoint()
    {
        if (flightReticle == null)
            return;

        if (flightCamera == null)
            flightCamera = Camera.main;

        if (flightCamera == null)
            return;

        // Get the Canvas containing the reticle.
        Canvas parentCanvas =
            flightReticle.GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            return;

        // Convert the reticle's position into screen coordinates.
        Camera canvasCamera =
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        if (canvasCamera == null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = flightCamera;
        }

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                canvasCamera,
                flightReticle.position
            );

        // Cast a ray through the reticle.
        Ray aimRay = flightCamera.ScreenPointToRay(screenPosition);

        // Default to a distant point along the ray.
        currentAimPoint = aimRay.GetPoint(aimRayDistance);

        // Find the closest valid hit, ignoring the player's own colliders.
        int hitCount = Physics.RaycastNonAlloc(
            aimRay,
            aimHits,
            aimRayDistance,
            aimLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = aimHits[i];

            if (hit.transform == null)
                continue;

            // Ignore the player's own colliders.
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                currentAimPoint = hit.point;
            }
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

        if (weapon.bulletPrefab == null ||
            weapon.firingPositions == null ||
            weapon.firingPositions.Length == 0)
            return;

        // Set this weapon's next firing time.
        weapon.nextFireTime =
            Time.time + Mathf.Max(weapon.fireRate, 0.01f);

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
            FireBullet(
                weapon,
                weapon.firingPositions[weapon.currentFiringPosition]
            );

            // Move to the next firing position.
            weapon.currentFiringPosition++;

            if (weapon.currentFiringPosition >=
                weapon.firingPositions.Length)
            {
                weapon.currentFiringPosition = 0;
            }
        }
    }

    private void FireBullet(Weapon weapon, Transform firingPosition)
    {
        if (firingPosition == null)
            return;

        // Calculate the direction from this muzzle to the aiming point.
        Vector3 firingDirection =
            (currentAimPoint - firingPosition.position).normalized;

        if (firingDirection.sqrMagnitude < 0.001f)
            return;

        // Rotate the projectile to face the aiming direction.
        Quaternion firingRotation = Quaternion.LookRotation(
            firingDirection,
            firingPosition.up
        );

        // Instantiate bullet at the firing position.
        GameObject bullet = Instantiate(
            weapon.bulletPrefab,
            firingPosition.position,
            firingRotation
        );

        // Apply bullet velocity, including player momentum.
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            Vector3 playerVelocity = playerRigidbody != null
                ? playerRigidbody.linearVelocity
                : Vector3.zero;

            Vector3 firingVelocity =
                firingDirection * weapon.bulletSpeed;

            bulletRb.linearVelocity =
                playerVelocity + firingVelocity;
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
