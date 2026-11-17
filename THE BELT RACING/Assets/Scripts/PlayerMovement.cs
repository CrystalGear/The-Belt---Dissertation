
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public event Action<int> OnSpeedChanged;
    public event Action<float> OnEnergyChanged;

    [Header("Movement")]
    [SerializeField] float baseSpeed = 60f;

    [Header("Boost")]
    [SerializeField] float boostSpeed = 2f;
    [SerializeField] float maxBoostEnergy = 100f;
    [SerializeField] float boostConsumptionRate = 1f;
    [SerializeField] float boostRegenRate = 1f;

    [Header("Reticle Steering")]
    [SerializeField] RectTransform flightReticle;
    [SerializeField] Camera flightCamera;

    [SerializeField, Range(0.1f, 0.5f)]
    float reticleMovementRadius = 0.35f;

    [SerializeField, Range(5f, 80f)]
    float maxSteeringAngle = 35f;

    [Header("Flight Control Sensitivity")]
    [SerializeField, Range(0f, 3f)]
    float pitchSensitivity = 1f;

    [SerializeField, Range(0f, 3f)]
    float yawSensitivity = 1f;

    [SerializeField, Range(0f, 3f)]
    float rollSensitivity = 1f;

    [Header("Flight Control Strength")]
    [SerializeField] float pitchStrength = 1f;
    [SerializeField] float yawStrength = 3f;
    [SerializeField] float rollStrength = 2f;

    [Header("Automatic Banking")]
    [SerializeField] bool autoBanking = true;

    [Tooltip("Maximum angle the ship will bank into a turn.")]
    [SerializeField, Range(0f, 80f)]
    float maxBankAngle = 30f;

    [Tooltip("How quickly the ship rolls towards its target bank angle.")]
    [SerializeField] float autoBankStrength = 2f;

    [Tooltip("How strongly unwanted rolling is damped.")]
    [SerializeField] float autoBankDamping = 1.5f;

    [Header("Serialized for debugging")]
    [SerializeField] bool isBoosting;
    [SerializeField] float accelerationInput;
    [SerializeField] float brakeInput;
    [SerializeField] Vector2 steeringInput;
    [SerializeField] float rollLeftInput;
    [SerializeField] float rollRightInput;
    [SerializeField] float currentBoostEnergy = 100f;
    [SerializeField] float currentBankAngle;

    Rigidbody rigidbody;

    private float EnergyNormalised =>
        maxBoostEnergy > 0f
        ? currentBoostEnergy / maxBoostEnergy
        : 0f;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        if (flightCamera == null)
            flightCamera = Camera.main;
    }

    public Vector2 GetSteeringInput()
    {
        return steeringInput;
    }

    private void FixedUpdate()
    {
        UpdateReticleSteering();

        ApplyMovement();
        ApplySteering();
        ApplyAutoBank();
        ApplyRoll();

        BoostLogic();
        ReportSpeed();
    }

    private void UpdateReticleSteering()
    {
        if (flightReticle == null)
            return;

        if (flightCamera == null)
            flightCamera = Camera.main;

        if (flightCamera == null)
            return;

        Canvas parentCanvas = flightReticle.GetComponentInParent<Canvas>();

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

        float forwardAmount = Mathf.Max(0.1f, localDirection.z);

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

    private void ApplyAutoBank()
    {
        if (!autoBanking)
            return;

        // Check whether the player is manually controlling roll.
        float manualRoll = rollLeftInput - rollRightInput;

        // Give manual roll complete priority.
        if (Mathf.Abs(manualRoll) > 0.01f)
            return;

        // Get angular velocity in ship-local space.
        Vector3 localAngularVelocity =
            transform.InverseTransformDirection(
                rigidbody.angularVelocity
            );

        // Work out the ship's bank relative to its heading.
        Vector3 levelUp = Vector3.ProjectOnPlane(
            Vector3.up,
            transform.forward
        );

        if (levelUp.sqrMagnitude < 0.001f)
            return;

        levelUp.Normalize();

        currentBankAngle = Vector3.SignedAngle(
            levelUp,
            transform.up,
            transform.forward
        );

        // Calculate the desired bank angle from horizontal steering.
        float targetBankAngle =
            -steeringInput.x * maxBankAngle;

        // Calculate the error between current and desired bank.
        float bankError = Mathf.DeltaAngle(
            currentBankAngle,
            targetBankAngle
        );

        // Correct the bank angle while damping unwanted roll velocity.
        float bankTorque =
            (bankError * autoBankStrength)
            - (localAngularVelocity.z * autoBankDamping);

        rigidbody.AddRelativeTorque(
            Vector3.forward * bankTorque,
            ForceMode.Acceleration
        );
    }

    private void ApplySteering()
    {
        Vector3 deltaTorque = Vector3.zero;

        // Pitch: Up and down.
        deltaTorque.x =
            -steeringInput.y
            * pitchSensitivity
            * pitchStrength;

        // Yaw: Left and right (Y axis).
        deltaTorque.y =
            steeringInput.x
            * yawSensitivity
            * yawStrength;

        // Apply pitch and yaw independently of Rigidbody mass.
        rigidbody.AddRelativeTorque(
            deltaTorque,
            ForceMode.Acceleration
        );
    }

    private void ApplyRoll()
    {
        Vector3 deltaRoll = Vector3.zero;

        // Roll: Rotate around the Z axis.
        deltaRoll.z =
            (rollLeftInput - rollRightInput)
            * rollSensitivity
            * rollStrength;

        rigidbody.AddRelativeTorque(deltaRoll);
    }

    private void ApplyMovement()
    {
        float deltaForce = accelerationInput - brakeInput;
        deltaForce *= baseSpeed;

        Vector3 deltaVector = transform.forward * deltaForce;

        rigidbody.AddForce(deltaVector);
    }

    private void BoostLogic()
    {
        if (isBoosting)
        {
            currentBoostEnergy -=
                boostConsumptionRate * Time.fixedDeltaTime;

            Vector3 deltaVector = transform.forward * boostSpeed;
            rigidbody.AddForce(deltaVector);

            if (currentBoostEnergy <= 0f)
            {
                currentBoostEnergy = 0f;
                isBoosting = false;
            }
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

        OnEnergyChanged?.Invoke(EnergyNormalised);
    }

    private void ReportSpeed()
    {
        Vector3 velocity = rigidbody.linearVelocity;

        OnSpeedChanged?.Invoke((int)velocity.magnitude);
    }

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
        isBoosting = iv.isPressed;
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
