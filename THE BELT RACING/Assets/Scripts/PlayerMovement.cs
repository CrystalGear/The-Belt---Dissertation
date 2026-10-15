using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public event Action<int> OnSpeedChanged;
    public event Action<float> OnEnergyChanged;

    [SerializeField] float baseSpeed = 60f;
    [SerializeField] float boostSpeed = 2f;
    [SerializeField] float maxBoostEnergy = 100f;
    [SerializeField] float boostConsumptionRate =1f;
    [SerializeField] float boostRegenRate = 1f;


    [Header("Serialized for debugging")]
    [SerializeField] bool isBoosting;
    [SerializeField] float accelerationInput;
    [SerializeField] float brakeInput;
    [SerializeField] Vector2 steeringInput;
    [SerializeField] float rollLeftInput;
    [SerializeField] float rollRightInput;
    [SerializeField] float currentBoostEnergy = 100f;

    Rigidbody rigidbody;
    private float EnergyNormalised => currentBoostEnergy/maxBoostEnergy;

    //apply force to the rigidbody using coast speed
    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        //rigidbody.maxLinearVelocity = 344;      // Set max linear velocity tomach 1
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
                isBoosting = false;
            }
        }
        else
        {
            currentBoostEnergy += boostRegenRate * Time.fixedDeltaTime;
            currentBoostEnergy = Mathf.Clamp(currentBoostEnergy,0f,maxBoostEnergy);
        }
        OnEnergyChanged?.Invoke(EnergyNormalised);
    }

    private void ApplyRoll()
    {
        Vector3 deltaRoll = new();
        deltaRoll.z = rollLeftInput - rollRightInput;
        rigidbody.AddRelativeTorque(deltaRoll);
    }

    private void ApplySteering()
    {
        Vector3 deltaTorque = new();
        deltaTorque.x = -steeringInput.y;
        deltaTorque.y = steeringInput.x;
        rigidbody.AddRelativeTorque(deltaTorque);
    }

    private void ApplyMovement()
    {
        float deltaForce = accelerationInput -brakeInput;
        deltaForce = deltaForce * baseSpeed;

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
