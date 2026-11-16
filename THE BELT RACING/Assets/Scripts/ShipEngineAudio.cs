
using UnityEngine;

public class ShipEngineAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] AudioSource engineIdleSource;
    [SerializeField] AudioSource thrusterSource;

    [Header("Ship Movement")]
    [SerializeField] Rigidbody shipRigidbody;

    [Header("Speed Settings")]
    [SerializeField] float maximumSpeed = 100f;

    [Header("Idle Engine Settings")]
    [SerializeField] float idleMinimumPitch = 0.8f;
    [SerializeField] float idleMaximumPitch = 1.2f;

    [SerializeField] float idleMaximumVolume = 0.4f;
    [SerializeField] float idleMinimumVolume = 0.05f;

    [Header("Thruster Settings")]
    [SerializeField] float thrusterMinimumPitch = 0.8f;
    [SerializeField] float thrusterMaximumPitch = 2f;

    [SerializeField] float thrusterMinimumVolume = 0f;
    [SerializeField] float thrusterMaximumVolume = 1f;

    [Header("Smoothing")]
    [SerializeField] float audioSmoothSpeed = 3f;

    float speedNormalised;

    private void Awake()
    {
        if (shipRigidbody == null)
            shipRigidbody = GetComponentInParent<Rigidbody>();

        SetupAudioSource(engineIdleSource);
        SetupAudioSource(thrusterSource);
    }

    private void SetupAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        source.loop = true;
        source.playOnAwake = true;

        if (!source.isPlaying)
            source.Play();
    }

    private void Update()
    {
        if (shipRigidbody == null)
            return;

        // Get current ship speed.
        float currentSpeed = shipRigidbody.linearVelocity.magnitude;

        // Normalise speed between 0 and 1.
        speedNormalised = Mathf.Clamp01(currentSpeed / maximumSpeed);

        UpdateIdleEngine();
        UpdateThrusters();
    }

    private void UpdateIdleEngine()
    {
        if (engineIdleSource == null)
            return;

        // Idle pitch increases slightly with speed.
        float targetPitch = Mathf.Lerp(
            idleMinimumPitch,
            idleMaximumPitch,
            speedNormalised
        );

        // Idle volume fades out as speed increases.
        float targetVolume = Mathf.Lerp(
            idleMaximumVolume,
            idleMinimumVolume,
            speedNormalised
        );

        engineIdleSource.pitch = Mathf.Lerp(
            engineIdleSource.pitch,
            targetPitch,
            audioSmoothSpeed * Time.deltaTime
        );

        engineIdleSource.volume = Mathf.Lerp(
            engineIdleSource.volume,
            targetVolume,
            audioSmoothSpeed * Time.deltaTime
        );
    }

    private void UpdateThrusters()
    {
        if (thrusterSource == null)
            return;

        // Thruster pitch increases with speed.
        float targetPitch = Mathf.Lerp(
            thrusterMinimumPitch,
            thrusterMaximumPitch,
            speedNormalised
        );

        // Thruster volume fades in as speed increases.
        float targetVolume = Mathf.Lerp(
            thrusterMinimumVolume,
            thrusterMaximumVolume,
            speedNormalised
        );

        thrusterSource.pitch = Mathf.Lerp(
            thrusterSource.pitch,
            targetPitch,
            audioSmoothSpeed * Time.deltaTime
        );

        thrusterSource.volume = Mathf.Lerp(
            thrusterSource.volume,
            targetVolume,
            audioSmoothSpeed * Time.deltaTime
        );
    }
}
