using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float baseSpeed = 60f;
    [SerializeField] float coastSpeed = 10f;
    [SerializeField] float brakeSpeed = 20f;
    [SerializeField] float brakeDeceleration = 45f;
    [SerializeField] float turnRate = 120f;
    [SerializeField] float steerResponsiveness = 12f;

    public void OnAcceleration()
    {

    }
    public void OnBrake()
    {

    }
    public void OnBoost() 
    {
        
    }
    public void OnSteering()
    {
        
    }
}
