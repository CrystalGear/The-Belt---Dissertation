using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float baseSpeed = 60f;
    [SerializeField] float brakeSpeed = 20f;
    [SerializeField] float brakeDeceleration = 45f;
    [SerializeField] float turnRate = 120f;
    [SerializeField] float steerResponsiveness = 12f;
    [SerializeField] float boostSpeed = 2f;
    [SerializeField] float energy;

    [Header("Serialized for debugging")]
    [SerializeField] bool isBoosting;
    [SerializeField] float accelerationInput;
    [SerializeField] float brakeInput;
    [SerializeField] Vector2 steeringInput;

    Rigidbody rigidbody;

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
    }

    private void ApplySteering()
    {
        Vector3 deltaTorque = new();
        deltaTorque.x = steeringInput.y;
        deltaTorque.y = steeringInput.x;
        //deltaTorque.z = steeringInput.x * 0.25f;

        rigidbody.AddRelativeTorque(deltaTorque);
    }

    private void ApplyMovement()
    {
        // brake input needs to be nagtive not positive
        //if both are applied they cancel each out
        //acceleration is the poisive input
        float deltaForce = accelerationInput -brakeInput;
        deltaForce = deltaForce * baseSpeed;

        Vector3 deltaVector = transform.forward * deltaForce;

        rigidbody.AddForce(deltaVector);


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
    #endregion
}
