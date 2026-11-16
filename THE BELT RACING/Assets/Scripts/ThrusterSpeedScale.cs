
using UnityEngine;

public class ThrusterSpeedScale : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody parentRigidbody;

    [Header("Speed Settings")]
    [SerializeField] private float minimumSpeed = 0f;
    [SerializeField] private float maximumSpeed = 100f;

    [Header("Scale Settings")]
    [SerializeField] private float minimumScaleY = 0.5f;
    [SerializeField] private float maximumScaleY = 3f;

    [SerializeField] private float smoothingSpeed = 5f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (parentRigidbody == null)
        {
            parentRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (parentRigidbody == null)
            return;

        // Get parent object's current speed
        float currentSpeed = parentRigidbody.linearVelocity.magnitude;

        // Convert speed into a 0-1 range
        float speedPercentage = Mathf.InverseLerp(
            minimumSpeed,
            maximumSpeed,
            currentSpeed
        );

        // Calculate target Y scale
        float targetScaleY = Mathf.Lerp(
            minimumScaleY,
            maximumScaleY,
            speedPercentage
        );

        // Smoothly interpolate scale
        Vector3 targetScale = new Vector3(
            originalScale.x,
            originalScale.y * targetScaleY,
            originalScale.z
        );

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * smoothingSpeed
        );
    }
}
