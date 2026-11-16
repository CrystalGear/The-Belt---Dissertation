
using UnityEngine;

public class SpeedParticleController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("The player's Rigidbody.")]
    [SerializeField] Rigidbody playerRigidbody;

    [Header("Particle System")]
    [SerializeField] ParticleSystem speedParticles;

    [Header("Speed Settings")]
    [SerializeField] float activationSpeed = 40f;
    [SerializeField] float maximumSpeed = 100f;

    [Header("Particle Size")]
    [Tooltip("Enable speed-based particle sizing.")]
    [SerializeField] bool controlParticleSize = true;

    [Tooltip("Particle size at activation speed.")]
    [SerializeField] float minimumParticleSize = 0.1f;

    [Tooltip("Particle size at maximum speed.")]
    [SerializeField] float maximumParticleSize = 2f;

    [Header("Particle Emission")]
    [Tooltip("Enable speed-based emission rate.")]
    [SerializeField] bool controlEmission = true;

    [Tooltip("Particles emitted per second at activation speed.")]
    [SerializeField] float minimumEmission = 5f;

    [Tooltip("Particles emitted per second at maximum speed.")]
    [SerializeField] float maximumEmission = 100f;

    [Header("Transform Scale")]
    [Tooltip("Enable scaling of the entire particle system.")]
    [SerializeField] bool controlTransformScale = false;

    [SerializeField] float minimumScale = 0.5f;
    [SerializeField] float maximumScale = 3f;

    [Header("Transition")]
    [Tooltip("How quickly the effect responds to speed changes.")]
    [SerializeField] float transitionSpeed = 3f;

    [Header("Options")]
    [SerializeField] bool stopWhenBelowSpeed = true;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;

    private Vector3 originalScale;
    private float originalStartSize;
    private float originalEmission;

    private float currentIntensity;
    private bool isPlaying;

    private void Awake()
    {
        if (speedParticles == null)
            speedParticles = GetComponent<ParticleSystem>();

        if (speedParticles == null)
        {
            Debug.LogWarning("SpeedParticleController: No Particle System assigned!", this);
            enabled = false;
            return;
        }

        mainModule = speedParticles.main;
        emissionModule = speedParticles.emission;

        originalStartSize = mainModule.startSize.constant;
        originalEmission = emissionModule.rateOverTime.constant;

        originalScale = speedParticles.transform.localScale;

        speedParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void Update()
    {
        if (playerRigidbody == null)
            return;

        float currentSpeed = playerRigidbody.linearVelocity.magnitude;

        float targetIntensity = Mathf.InverseLerp(
            activationSpeed,
            maximumSpeed,
            currentSpeed
        );

        currentIntensity = Mathf.MoveTowards(
            currentIntensity,
            targetIntensity,
            transitionSpeed * Time.deltaTime
        );

        // Start particle system.
        if (currentSpeed >= activationSpeed && !isPlaying)
        {
            speedParticles.Play();
            isPlaying = true;
        }

        // Stop particle system.
        if (currentSpeed < activationSpeed && stopWhenBelowSpeed && isPlaying)
        {
            speedParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );

            isPlaying = false;
        }

        UpdateParticleSize();
        UpdateEmission();
        UpdateTransformScale();
    }

    private void UpdateParticleSize()
    {
        if (!controlParticleSize)
            return;

        float size = Mathf.Lerp(
            minimumParticleSize,
            maximumParticleSize,
            currentIntensity
        );

        mainModule.startSize = size;
    }

    private void UpdateEmission()
    {
        if (!controlEmission)
            return;

        float emission = Mathf.Lerp(
            minimumEmission,
            maximumEmission,
            currentIntensity
        );

        emissionModule.rateOverTime = emission;
    }

    private void UpdateTransformScale()
    {
        if (!controlTransformScale)
            return;

        float scale = Mathf.Lerp(
            minimumScale,
            maximumScale,
            currentIntensity
        );

        speedParticles.transform.localScale = originalScale * scale;
    }
}
