
using UnityEngine;

public class ShipSpeedFOV : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera targetCamera;
    [SerializeField] Rigidbody playerRigidbody;

    [Header("Speed Settings")]
    [Tooltip("Speed at which the camera reaches its maximum FOV.")]
    [SerializeField] float maximumSpeed = 100f;

    [Header("FOV Settings")]
    [SerializeField] float minimumFOV = 60f;
    [SerializeField] float maximumFOV = 85f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera adjusts its FOV.")]
    [SerializeField] float fovSmoothSpeed = 3f;

    float targetFOV;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (playerRigidbody == null)
            playerRigidbody = GetComponentInParent<Rigidbody>();

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = minimumFOV;
            targetFOV = minimumFOV;
        }
    }

    private void Update()
    {
        if (targetCamera == null || playerRigidbody == null)
            return;

        // Get the ship's current speed.
        float currentSpeed = playerRigidbody.linearVelocity.magnitude;

        // Convert speed into a value between 0 and 1.
        float speedNormalised = Mathf.Clamp01(currentSpeed / maximumSpeed);

        // Calculate the desired FOV.
        targetFOV = Mathf.Lerp(minimumFOV, maximumFOV, speedNormalised);

        // Smoothly adjust the camera FOV.
        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            targetFOV,
            fovSmoothSpeed * Time.deltaTime
        );
    }
}
