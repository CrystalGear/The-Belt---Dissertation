
using UnityEngine;

public class CameraVelocityShake: MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's Rigidbody used to calculate velocity.")]
    public Rigidbody playerRigidbody;

    [Tooltip("The camera transform to apply shake to.")]
    public Transform cameraTransform;

    [Header("Speed Settings")]
    [Tooltip("Speed at which the camera reaches maximum shake.")]
    public float maximumShakeSpeed = 100f;

    [Tooltip("Speed at which camera shake begins.")]
    public float shakeStartSpeed = 20f;

    [Header("Shake Settings")]
    [Tooltip("Maximum positional vibration strength.")]
    public float maximumShakeAmount = 0.04f;

    [Tooltip("How quickly the vibration changes.")]
    public float shakeFrequency = 20f;

    [Tooltip("How quickly shake increases and decreases.")]
    public float shakeSmoothness = 3f;

    [Header("Rotation Shake")]
    [Tooltip("Maximum rotational vibration in degrees.")]
    public float rotationShakeAmount = 0.3f;

    [Header("Debug")]
    [SerializeField] float currentSpeed;
    [SerializeField] float currentShake;

    Vector3 initialLocalPosition;
    Quaternion initialLocalRotation;

    float shakeOffset;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        initialLocalPosition = cameraTransform.localPosition;
        initialLocalRotation = cameraTransform.localRotation;

        shakeOffset = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        if (playerRigidbody == null || cameraTransform == null)
            return;

        // Get the player's current velocity.
        currentSpeed = playerRigidbody.linearVelocity.magnitude;

        // Calculate shake intensity based on speed.
        float speedPercentage = Mathf.InverseLerp(
            shakeStartSpeed,
            maximumShakeSpeed,
            currentSpeed
        );

        // Smoothly transition the shake intensity.
        currentShake = Mathf.Lerp(
            currentShake,
            speedPercentage,
            Time.deltaTime * shakeSmoothness
        );

        // Generate smooth Perlin noise.
        float time = Time.time * shakeFrequency + shakeOffset;

        float shakeX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f;
        float shakeY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f;
        float shakeZ = (Mathf.PerlinNoise(time, time) - 0.5f) * 2f;

        // Apply positional vibration.
        Vector3 positionShake = new Vector3(
            shakeX,
            shakeY,
            shakeZ
        ) * maximumShakeAmount * currentShake;

        cameraTransform.localPosition =
            initialLocalPosition + positionShake;

        // Apply subtle rotational vibration.
        Vector3 rotationShake = new Vector3(
            shakeY,
            shakeX,
            shakeZ
        ) * rotationShakeAmount * currentShake;

        cameraTransform.localRotation =
            initialLocalRotation * Quaternion.Euler(rotationShake);
    }
}
