
using UnityEngine;

public class PlayerWingTilt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationAngle = 20f;
    [SerializeField] private float rotationSpeed = 5f;

    private Quaternion leftWingInitialRotation;
    private Quaternion rightWingInitialRotation;

    private void Start()
    {
        // Store the original local rotations
        leftWingInitialRotation = leftWing.localRotation;
        rightWingInitialRotation = rightWing.localRotation;
    }

    private void Update()
    {
        // Get steering input from the player movement script
        float steering = playerMovement.GetSteeringInput().x;

        // Calculate target rotations around local X axis
        Quaternion leftTarget = leftWingInitialRotation *
            Quaternion.Euler(-steering * rotationAngle, 0f, 0f);

        Quaternion rightTarget = rightWingInitialRotation *
            Quaternion.Euler(steering * rotationAngle, 0f, 0f);

        // Smoothly rotate the wings
        leftWing.localRotation = Quaternion.Lerp(
            leftWing.localRotation,
            leftTarget,
            rotationSpeed * Time.deltaTime
        );

        rightWing.localRotation = Quaternion.Lerp(
            rightWing.localRotation,
            rightTarget,
            rotationSpeed * Time.deltaTime
        );
    }
}
