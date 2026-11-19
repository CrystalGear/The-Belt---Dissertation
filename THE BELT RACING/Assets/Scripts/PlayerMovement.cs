
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public event Action<int> OnSpeedChanged;
    public event Action<float> OnEnergyChanged;
    public event Action OnBoostReady;

    [Header("Movement")]
    [SerializeField] private float baseSpeed = 60f;

    [Header("Boost")]
    [SerializeField] private float boostSpeed = 2f;
    [SerializeField] private float maxBoostEnergy = 100f;
    [SerializeField] private float boostConsumptionRate = 1f;
    [SerializeField] private float boostRegenRate = 1f;

    [Tooltip("Time to wait after boost depletion before regeneration starts.")]
    [SerializeField] private float boostRechargeDelay = 2f;

    [Header("Boost Audio")]
    [Tooltip("Audio played once when boost energy is depleted and engines overheat.")]
    [SerializeField] private AudioSource boostOverheatAudioSource;

    [Header("Boost Overheat Penalty")]
    [Tooltip("Percentage of forward speed lost when boost overheats. 0.35 = 35% loss.")]
    [Range(0f, 1f)]
    [SerializeField] private float overheatSpeedLoss = 0.35f;

    [Tooltip("Minimum forward speed the ship retains after overheating.")]
    [SerializeField] private float minimumSpeedAfterOverheat = 15f;

    [Header("Reticle Steering")]
    [SerializeField] private RectTransform flightReticle;
    [SerializeField] private Camera flightCamera;

    [SerializeField, Range(0.1f, 0.5f)]
    private float reticleMovementRadius = 0.35f;

    [SerializeField, Range(5f, 80f)]
    private float maxSteeringAngle = 35f;

    [Header("Flight Control Sensitivity")]
    [SerializeField, Range(0f, 3f)]
    private float pitchSensitivity = 1f;

    [SerializeField, Range(0f, 3f)]
    private float yawSensitivity = 1f;

    [SerializeField, Range(0f, 3f)]
    private float rollSensitivity = 1f;

    [Header("Maximum Rotation Speeds")]
    [Tooltip("Maximum pitch rotation speed in degrees per second.")]
    [SerializeField] private float maxPitchRate = 90f;

    [Tooltip("Maximum yaw rotation speed in degrees per second.")]
    [SerializeField] private float maxYawRate = 120f;

    [Tooltip("Maximum manual roll speed in degrees per second.")]
    [SerializeField] private float maxRollRate = 120f;

    [Header("Flight Stability")]
    [Tooltip("How quickly the ship reaches its requested rotation speed.")]
    [SerializeField] private float rotationResponse = 8f;

    [Tooltip("Maximum angular acceleration in degrees per second squared.")]
    [SerializeField] private float maxAngularAcceleration = 360f;

    [Header("Rotational Damping")]
    [Tooltip("How quickly unwanted rotation is removed when there is no input.")]
    [SerializeField] private float rotationalDamping = 4f;

    [Tooltip("Rotation speed below which tiny residual rotation is removed.")]
    [SerializeField] private float rotationDeadzone = 0.05f;

    [Header("Automatic Banking")]
    [SerializeField] private bool autoBanking = true;

    [Tooltip("Maximum angle the ship will bank into a turn.")]
    [SerializeField, Range(0f, 80f)]
    private float maxBankAngle = 30f;

    [Tooltip("How quickly the ship attempts to reach its target bank angle.")]
    [SerializeField] private float autoBankStrength = 3f;

    [Tooltip("Maximum automatic banking rotation speed.")]
    [SerializeField] private float maxAutoBankRate = 90f;

    [Header("Serialized for Debugging")]
    [SerializeField] private bool isBoosting;
    [SerializeField] private bool enginesOverheated;
    [SerializeField] private float boostRechargeTimer;
    [SerializeField] private float accelerationInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private Vector2 steeringInput;
    [SerializeField] private float rollLeftInput;
    [SerializeField] private float rollRightInput;
    [SerializeField] private float currentBoostEnergy = 100f;
    [SerializeField] private float currentBankAngle;

    private Rigidbody rb;

    // UI accessors.
    public float CurrentBoostEnergy => currentBoostEnergy;
    public float MaxBoostEnergy => maxBoostEnergy;
    public bool EnginesOverheated => enginesOverheated;

    public bool CanUseBoost =>
        !enginesOverheated && currentBoostEnergy > 0f;

    public bool IsBoostRecharging =>
        currentBoostEnergy < maxBoostEnergy;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (flightCamera == null)
            flightCamera = Camera.main;

        maxBoostEnergy = Mathf.Max(0f, maxBoostEnergy);

        currentBoostEnergy = Mathf.Clamp(
            currentBoostEnergy,
            0f,
            maxBoostEnergy
        );
    }

    private void Start()
    {
        OnEnergyChanged?.Invoke(currentBoostEnergy);
    }

    public Vector2 GetSteeringInput()
    {
        return steeringInput;
    }

    private void FixedUpdate()
    {
        UpdateReticleSteering();

        ApplyMovement();
        ApplyFlightControl();

        BoostLogic();
        ReportSpeed();
    }

    #region Steering

    private void UpdateReticleSteering()
    {
        if (flightReticle == null)
            return;

        if (flightCamera == null)
            flightCamera = Camera.main;

        if (flightCamera == null)
            return;

        Canvas parentCanvas =
            flightReticle.GetComponentInParent<Canvas>();

        float canvasScale = parentCanvas != null
            ? parentCanvas.scaleFactor
            : 1f;

        float maxRadius =
            Screen.height * reticleMovementRadius / canvasScale;

        if (maxRadius <= 0f)
            return;

        Vector2 reticleOffset = flightReticle.anchoredPosition;

        Vector2 normalisedOffset = reticleOffset / maxRadius;

        normalisedOffset = Vector2.ClampMagnitude(
            normalisedOffset,
            1f
        );

        float angle = maxSteeringAngle * Mathf.Deg2Rad;
        float angleScale = Mathf.Tan(angle);

        Vector3 desiredDirection =
            transform.forward
            + flightCamera.transform.right *
                (normalisedOffset.x * angleScale)
            + flightCamera.transform.up *
                (normalisedOffset.y * angleScale);

        Vector3 localDirection =
            transform.InverseTransformDirection(
                desiredDirection.normalized
            );

        float forwardAmount = Mathf.Max(
            0.1f,
            localDirection.z
        );

        steeringInput.x = Mathf.Clamp(
            localDirection.x / forwardAmount / angleScale,
            -1f,
            1f
        );

        steeringInput.y = Mathf.Clamp(
            localDirection.y / forwardAmount / angleScale,
            -1f,
            1f
        );
    }

    #endregion

    #region Flight Control

    private void ApplyFlightControl()
    {
        Vector3 localAngularVelocity =
            transform.InverseTransformDirection(
                rb.angularVelocity
            );

        Vector3 targetAngularVelocity = Vector3.zero;

        // Pitch.
        targetAngularVelocity.x =
            -steeringInput.y
            * pitchSensitivity
            * maxPitchRate
            * Mathf.Deg2Rad;

        // Yaw.
        targetAngularVelocity.y =
            steeringInput.x
            * yawSensitivity
            * maxYawRate
            * Mathf.Deg2Rad;

        // Roll.
        float manualRoll = rollLeftInput - rollRightInput;

        if (Mathf.Abs(manualRoll) > 0.01f)
        {
            targetAngularVelocity.z =
                manualRoll
                * rollSensitivity
                * maxRollRate
                * Mathf.Deg2Rad;
        }
        else if (autoBanking)
        {
            targetAngularVelocity.z = CalculateAutoBankRate();
        }

        Vector3 angularVelocityError =
            targetAngularVelocity - localAngularVelocity;

        Vector3 angularAcceleration =
            angularVelocityError * rotationResponse;

        bool hasRotationInput =
            Mathf.Abs(steeringInput.x) > 0.01f ||
            Mathf.Abs(steeringInput.y) > 0.01f ||
            Mathf.Abs(manualRoll) > 0.01f;

        if (!hasRotationInput)
        {
            angularAcceleration -=
                localAngularVelocity * rotationalDamping;

            if (localAngularVelocity.sqrMagnitude <
                rotationDeadzone * rotationDeadzone)
            {
                angularAcceleration -=
                    localAngularVelocity * 10f;
            }
        }

        float maxAcceleration =
            maxAngularAcceleration * Mathf.Deg2Rad;

        angularAcceleration = Vector3.ClampMagnitude(
            angularAcceleration,
            maxAcceleration
        );

        rb.AddRelativeTorque(
            angularAcceleration,
            ForceMode.Acceleration
        );
    }

    private float CalculateAutoBankRate()
    {
        Vector3 levelUp = Vector3.ProjectOnPlane(
            Vector3.up,
            transform.forward
        );

        if (levelUp.sqrMagnitude < 0.001f)
            return 0f;

        levelUp.Normalize();

        currentBankAngle = Vector3.SignedAngle(
            levelUp,
            transform.up,
            transform.forward
        );

        float targetBankAngle =
            -steeringInput.x * maxBankAngle;

        float bankError = Mathf.DeltaAngle(
            currentBankAngle,
            targetBankAngle
        );

        float targetRollRate =
            bankError * autoBankStrength;

        return Mathf.Clamp(
            targetRollRate,
            -maxAutoBankRate,
            maxAutoBankRate
        ) * Mathf.Deg2Rad;
    }

    #endregion

    #region Movement

    private void ApplyMovement()
    {
        float deltaForce = accelerationInput - brakeInput;
        deltaForce *= baseSpeed;

        // Thrust is always applied in the direction the craft faces.
        Vector3 deltaVector = transform.forward * deltaForce;

        rb.AddForce(deltaVector);
    }

    #endregion

    #region Boost

    private void BoostLogic()
    {
        if (isBoosting && CanUseBoost)
        {
            currentBoostEnergy -=
                boostConsumptionRate * Time.fixedDeltaTime;

            Vector3 deltaVector = transform.forward * boostSpeed;

            rb.AddForce(deltaVector);

            if (currentBoostEnergy <= 0f)
            {
                currentBoostEnergy = 0f;

                enginesOverheated = true;
                isBoosting = false;

                boostRechargeTimer = boostRechargeDelay;

                ApplyOverheatSpeedPenalty();

                if (boostOverheatAudioSource != null)
                {
                    boostOverheatAudioSource.Play();
                }
            }
        }
        else
        {
            if (currentBoostEnergy < maxBoostEnergy)
            {
                if (enginesOverheated && boostRechargeTimer > 0f)
                {
                    boostRechargeTimer -= Time.fixedDeltaTime;

                    boostRechargeTimer = Mathf.Max(
                        boostRechargeTimer,
                        0f
                    );
                }
                else
                {
                    currentBoostEnergy +=
                        boostRegenRate * Time.fixedDeltaTime;

                    currentBoostEnergy = Mathf.Clamp(
                        currentBoostEnergy,
                        0f,
                        maxBoostEnergy
                    );
                }
            }

            if (enginesOverheated &&
                currentBoostEnergy >= maxBoostEnergy)
            {
                currentBoostEnergy = maxBoostEnergy;

                enginesOverheated = false;

                OnBoostReady?.Invoke();
            }
        }

        OnEnergyChanged?.Invoke(currentBoostEnergy);
    }

    private void ApplyOverheatSpeedPenalty()
    {
        if (rb == null)
            return;

        float currentForwardSpeed = Vector3.Dot(
            rb.linearVelocity,
            transform.forward
        );

        if (currentForwardSpeed <= 0f)
            return;

        float reducedSpeed =
            currentForwardSpeed * (1f - overheatSpeedLoss);

        reducedSpeed = Mathf.Min(
            currentForwardSpeed,
            Mathf.Max(reducedSpeed, minimumSpeedAfterOverheat)
        );

        float speedDifference =
            currentForwardSpeed - reducedSpeed;

        rb.linearVelocity -=
            transform.forward * speedDifference;
    }

    #endregion

    #region UI

    private void ReportSpeed()
    {
        Vector3 velocity = rb.linearVelocity;

        // Total velocity magnitude.
        float totalSpeed = velocity.magnitude;

        // Velocity along the direction the craft is facing.
        float forwardSpeed = Vector3.Dot(
            velocity,
            transform.forward
        );

        // Debugging information.
        Debug.Log(
            $"Total Speed: {totalSpeed:F2} | " +
            $"Forward Speed: {forwardSpeed:F2}"
        );

        OnSpeedChanged?.Invoke(
            (int)totalSpeed
        );
    }

    #endregion

    #region Inputs

    public void OnAcceleration(InputValue iv)
    {
        accelerationInput = iv.Get<float>();
    }

    public void OnBrake(InputValue iv)
    {
        brakeInput = iv.Get<float>();
    }

    public void OnBoost(InputValue iv)
    {
        if (iv.isPressed)
        {
            if (CanUseBoost)
                isBoosting = true;
        }
        else
        {
            isBoosting = false;
        }
    }

    public void OnSteering(InputValue iv)
    {
        steeringInput = iv.Get<Vector2>();
    }

    public void OnRollRight(InputValue iv)
    {
        rollRightInput = iv.Get<float>();
    }

    public void OnRollLeft(InputValue iv)
    {
        rollLeftInput = iv.Get<float>();
    }

    #endregion
}
