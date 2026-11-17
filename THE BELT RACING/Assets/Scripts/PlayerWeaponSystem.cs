
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

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

    [Header("Weapon Heat")]
    [Tooltip("Heat generated every second while firing.")]
    [SerializeField] float heatPerSecond = 20f;

    [Tooltip("How quickly weapons cool down.")]
    [SerializeField] float coolingSpeed = 25f;

    [Tooltip("Maximum heat before weapons overheat.")]
    [SerializeField] float maximumHeat = 100f;

    [Tooltip("Temperature at which the overheating warning appears.")]
    [SerializeField] float overheatWarningThreshold = 60f;

    [Header("Overheat Effect")]
    [Tooltip("How long the radial stays full before cooling begins.")]
    [SerializeField] float overheatHoldDuration = 1.5f;

    [Tooltip("Speed of the warm/hot flashing effect.")]
    [SerializeField] float overheatFlashSpeed = 6f;

    [Header("Overheat Audio")]
    [Tooltip("Audio played once when the weapons overheat.")]
    [SerializeField] AudioSource overheatAudioSource;

    [Header("Weapon Heat UI")]
    [Tooltip("Radial image displaying weapon heat.")]
    [SerializeField] Image heatRadialFill;

    [Tooltip("Temperature displayed in the centre of the radial.")]
    [SerializeField] TMP_Text temperatureText;

    [Tooltip("Warning displayed when weapons overheat.")]
    [SerializeField] TMP_Text overheatText;

    [Header("Heat Colours")]
    [Tooltip("Colour when weapons are completely cool.")]
    [SerializeField] Color coolColour = Color.cyan;

    [Tooltip("Colour when weapons are approaching overheating.")]
    [SerializeField] Color warmColour = Color.yellow;

    [Tooltip("Colour when weapons reach maximum heat.")]
    [SerializeField] Color hotColour = Color.red;

    [Tooltip("Temperature percentage at which warm colour starts.")]
    [Range(0f, 1f)]
    [SerializeField] float warmThreshold = 0.5f;

    [Tooltip("Temperature percentage at which hot colour starts.")]
    [Range(0f, 1f)]
    [SerializeField] float hotThreshold = 0.8f;

    [Header("Aiming")]
    [Tooltip("The UI flight reticle used to determine the aiming direction.")]
    [SerializeField] RectTransform flightReticle;

    [Tooltip("The camera used to cast the aiming ray.")]
    [SerializeField] Camera flightCamera;

    [Tooltip("Objects that can be targeted by the aiming ray.")]
    [SerializeField] LayerMask aimLayers = ~0;

    [Tooltip("Maximum distance used when nothing is hit.")]
    [SerializeField] float aimRayDistance = 2000f;

    [Header("Projectile Momentum")]
    [Tooltip("How much player velocity projectiles inherit.")]
    [SerializeField] float playerVelocityMultiplier = 1f;

    [Tooltip("Additional velocity added in the firing direction.")]
    [SerializeField] float additionalMuzzleVelocity = 100f;

    [Header("Debugging")]
    [SerializeField] bool isFiring;
    [SerializeField] bool isOverheated;
    [SerializeField] float currentHeat;
    [SerializeField] Vector3 currentAimPoint;

    float overheatHoldTimer;

    Rigidbody playerRigidbody;

    readonly RaycastHit[] aimHits = new RaycastHit[32];

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (flightCamera == null)
            flightCamera = Camera.main;

        // Initialise heat.
        maximumHeat = Mathf.Max(maximumHeat, 0f);
        currentHeat = Mathf.Clamp(currentHeat, 0f, maximumHeat);

        isOverheated = maximumHeat > 0f && currentHeat >= maximumHeat;

        if (isOverheated)
        {
            overheatHoldTimer = overheatHoldDuration;
        }

        UpdateHeatUI();
        UpdateAimPoint();
    }

    private void Update()
    {
        UpdateAimPoint();

        UpdateWeaponHeat();

        if (isFiring && !isOverheated)
        {
            TryFire();
        }
    }

    #region Weapon Heat

    private void UpdateWeaponHeat()
    {
        if (isOverheated)
        {
            // Keep heat at maximum while the overheat effect plays.
            if (overheatHoldTimer > 0f)
            {
                overheatHoldTimer -= Time.deltaTime;
                currentHeat = maximumHeat;
            }
            else
            {
                // Begin cooling after the hold duration.
                currentHeat -= coolingSpeed * Time.deltaTime;
                currentHeat = Mathf.Max(currentHeat, 0f);

                // Weapons become available again when completely cooled.
                if (currentHeat <= 0f)
                {
                    isOverheated = false;
                    overheatHoldTimer = 0f;
                }
            }
        }
        else if (isFiring)
        {
            // Generate heat while firing.
            currentHeat += heatPerSecond * Time.deltaTime;
            currentHeat = Mathf.Min(currentHeat, maximumHeat);

            // Overheat when maximum heat is reached.
            if (maximumHeat > 0f && currentHeat >= maximumHeat)
            {
                currentHeat = maximumHeat;
                isOverheated = true;

                // Start the full radial hold.
                overheatHoldTimer = overheatHoldDuration;

                // Play the overheat sound once.
                if (overheatAudioSource != null)
                {
                    overheatAudioSource.Play();
                }
            }
        }
        else
        {
            // Gradually cool down when not firing.
            currentHeat -= coolingSpeed * Time.deltaTime;
            currentHeat = Mathf.Max(currentHeat, 0f);
        }

        UpdateHeatUI();
    }

    private void UpdateHeatUI()
    {
        float heatPercentage = maximumHeat > 0f
            ? Mathf.Clamp01(currentHeat / maximumHeat)
            : 0f;

        // Update radial fill.
        if (heatRadialFill != null)
        {
            heatRadialFill.fillAmount = heatPercentage;

            Color heatColour;

            // Flash between warm and hot while the radial is held full.
            if (isOverheated && overheatHoldTimer > 0f)
            {
                float flash = Mathf.PingPong(
                    Time.time * overheatFlashSpeed,
                    1f
                );

                heatColour = Color.Lerp(
                    warmColour,
                    hotColour,
                    flash
                );
            }
            else if (heatPercentage < warmThreshold)
            {
                float t = warmThreshold > 0f
                    ? heatPercentage / warmThreshold
                    : 1f;

                heatColour = Color.Lerp(
                    coolColour,
                    warmColour,
                    t
                );
            }
            else
            {
                float t = hotThreshold > warmThreshold
                    ? Mathf.InverseLerp(
                        warmThreshold,
                        hotThreshold,
                        heatPercentage
                    )
                    : 1f;

                heatColour = Color.Lerp(
                    warmColour,
                    hotColour,
                    t
                );
            }

            heatRadialFill.color = heatColour;
        }

        // Update centre temperature display.
        if (temperatureText != null)
        {
            temperatureText.text =
                Mathf.RoundToInt(currentHeat) + "°";
        }

        // Update overheating warning.
        if (overheatText != null)
        {
            if (isOverheated)
            {
                overheatText.text = "OVERHEATED";
                overheatText.gameObject.SetActive(true);
            }
            else if (currentHeat >= overheatWarningThreshold)
            {
                overheatText.text = "OVERHEATING";
                overheatText.gameObject.SetActive(true);
            }
            else
            {
                overheatText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region Aiming

    private void UpdateAimPoint()
    {
        if (flightReticle == null)
            return;

        if (flightCamera == null)
            flightCamera = Camera.main;

        if (flightCamera == null)
            return;

        Canvas parentCanvas =
            flightReticle.GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            return;

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

        Ray aimRay = flightCamera.ScreenPointToRay(screenPosition);

        currentAimPoint = aimRay.GetPoint(aimRayDistance);

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

    #endregion

    #region Firing

    public void TryFire()
    {
        if (!isFiring || isOverheated)
            return;

        if (weapons == null)
            return;

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

        weapon.nextFireTime =
            Time.time + Mathf.Max(weapon.fireRate, 0.01f);

        if (weapon.fireAllPositions)
        {
            foreach (Transform firingPosition in weapon.firingPositions)
            {
                FireBullet(weapon, firingPosition);
            }
        }
        else
        {
            FireBullet(
                weapon,
                weapon.firingPositions[weapon.currentFiringPosition]
            );

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

        Vector3 firingDirection =
            (currentAimPoint - firingPosition.position).normalized;

        if (firingDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion firingRotation = Quaternion.LookRotation(
            firingDirection,
            firingPosition.up
        );

        GameObject bullet = Instantiate(
            weapon.bulletPrefab,
            firingPosition.position,
            firingRotation
        );

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            Vector3 playerVelocity = playerRigidbody != null
                ? playerRigidbody.linearVelocity
                : Vector3.zero;

            // Inherit the player's existing momentum.
            Vector3 inheritedVelocity =
                playerVelocity * playerVelocityMultiplier;

            // Add the projectile's own forward velocity.
            Vector3 muzzleVelocity =
                firingDirection *
                (weapon.bulletSpeed + additionalMuzzleVelocity);

            // Combine both velocities.
            bulletRb.linearVelocity =
                inheritedVelocity + muzzleVelocity;
        }
        else
        {
            Debug.LogWarning(
                "Bullet prefab is missing a Rigidbody!",
                bullet
            );
        }
    }

    #endregion

    #region Inputs

    public void OnFire(InputValue iv)
    {
        isFiring = iv.isPressed;

        if (isFiring && !isOverheated)
        {
            TryFire();
        }
    }

    #endregion
}
