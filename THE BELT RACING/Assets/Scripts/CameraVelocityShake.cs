
using UnityEngine;
using Unity.Cinemachine;

public class CameraVelocityShake : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player Rigidbody used to calculate velocity.")]
    [SerializeField] Rigidbody playerRigidbody;

    [Tooltip("Cinemachine camera.")]
    [SerializeField] CinemachineCamera cinemachineCamera;

    [Tooltip("Cinemachine noise component.")]
    [SerializeField] CinemachineBasicMultiChannelPerlin cameraNoise;

    [Header("Speed Settings")]
    [SerializeField] float maximumShakeSpeed = 100f;
    [SerializeField] float shakeStartSpeed = 20f;

    [Header("Shake Settings")]
    [SerializeField] float maximumShakeAmount = 0.5f;
    [SerializeField] float shakeFrequency = 2f;
    [SerializeField] float shakeSmoothness = 3f;

    [Header("Debug")]
    [SerializeField] float currentSpeed;
    [SerializeField] float currentShake;

    void Awake()
    {
        if (cinemachineCamera != null && cameraNoise == null)
        {
            cameraNoise = cinemachineCamera
                .GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    void LateUpdate()
    {
        if (playerRigidbody == null || cameraNoise == null)
            return;

        // Get player velocity.
        currentSpeed = playerRigidbody.linearVelocity.magnitude;

        // Calculate shake based on speed.
        float speedPercentage = Mathf.InverseLerp(
            shakeStartSpeed,
            maximumShakeSpeed,
            currentSpeed
        );

        // Smooth shake intensity.
        currentShake = Mathf.Lerp(
            currentShake,
            speedPercentage,
            Time.deltaTime * shakeSmoothness
        );

        // Apply shake through Cinemachine.
        cameraNoise.AmplitudeGain =
            currentShake * maximumShakeAmount;

        cameraNoise.FrequencyGain = shakeFrequency;
    }
}
