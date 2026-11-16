
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

    [Header("Flight Control Sensitivity")]
    [Tooltip("Sensitivity multiplier for pitching up and down.")]
    [SerializeField, Range(0f, 3f)]
    float pitchSensitivity = 1f;

    [Tooltip("Sensitivity multiplier for turning left and right.")]
    [SerializeField, Range(0f, 3f)]
    float yawSensitivity = 1f;

    [Tooltip("Sensitivity multiplier for rolling left and right.")]
    [SerializeField, Range(0f, 3f)]
    float rollSensitivity = 1f;

    [Header("Flight Control Strength")]
    [Tooltip("Maximum torque applied when pitching.")]
    [SerializeField] float pitchStrength = 1f;

    [Tooltip("Maximum torque applied when yawing.")]
    [SerializeField] float yawStrength = 1f;

    [Tooltip("Maximum torque applied when rolling.")]
    [SerializeField] float rollStrength = 1f;

    [Header("Serialized for debugging")]
    [SerializeField] bool isBoosting;
    [SerializeField] float accelerationInput;
    [SerializeField] float brakeInput;
    [SerializeField] Vector2 steeringInput;
    [SerializeField] float rollLeftInput;
    [SerializeField] float rollRightInput;
    [SerializeField] float currentBoostEnergy = 100f;

    Rigidbody rigidbody;

    private float EnergyNormalised =>
        maxBoostEnergy > 0f
        ? currentBoostEnergy / maxBoostEnergy
        : 0f;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public Vector2 GetSteeringInput()
    {
        return steeringInput;
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplySteering();
        ApplyRoll();
        BoostLogic();
        ReportSpeed();
    }

    private void BoostLogic()
    {
        if (isBoosting)
        {
            currentBoostEnergy -= boostConsumptionRate * Time.fixedDeltaTime;

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
            currentBoostEnergy += boostRegenRate * Time.fixedDeltaTime;

            currentBoostEnergy = Mathf.Clamp(
                currentBoostEnergy,
                0f,
                maxBoostEnergy
            );
        }

        OnEnergyChanged?.Invoke(EnergyNormalised);
    }

    private void ApplyRoll()
    {
        Vector3 deltaRoll = Vector3.zero;

        deltaRoll.z =
            (rollLeftInput - rollRightInput)
            * rollSensitivity
            * rollStrength;

        rigidbody.AddRelativeTorque(deltaRoll);
    }

    private void ApplySteering()
    {
        Vector3 deltaTorque = Vector3.zero;

        // Pitch: Up and down
        deltaTorque.x =
            -steeringInput.y
            * pitchSensitivity
            * pitchStrength;

        // Yaw: Left and right
        deltaTorque.y =
            steeringInput.x
            * yawSensitivity
            * yawStrength;

        rigidbody.AddRelativeTorque(deltaTorque);
    }

    private void ApplyMovement()
    {
        float deltaForce = accelerationInput - brakeInput;
        deltaForce *= baseSpeed;

        Vector3 deltaVector = transform.forward * deltaForce;

        rigidbody.AddForce(deltaVector);
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
