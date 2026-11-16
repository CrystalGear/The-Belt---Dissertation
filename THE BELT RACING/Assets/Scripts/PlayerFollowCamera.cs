
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerFollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] Transform target;
    [SerializeField] string playerTag = "Player";
    [SerializeField] bool autoFindPlayer = true;

    [Header("Position Settings")]
    [SerializeField] Vector3 followOffset = new Vector3(0f, 4f, -12f);
    [SerializeField] float positionSmoothTime = 0.15f;
    [SerializeField] float maxFollowSpeed = 100f;

    [Header("Rotation Settings")]
    [SerializeField] float lookAheadDistance = 10f;
    [SerializeField] float lookHeight = 1f;
    [SerializeField] float rotationSmoothSpeed = 5f;
    [SerializeField] bool useTargetUpDirection = true;

    [Header("Banking Settings")]
    [SerializeField] bool useBanking = true;
    [SerializeField] float bankAmount = 3f;
    [SerializeField] float maxBankAngle = 15f;
    [SerializeField] float bankSmoothSpeed = 5f;

    [Header("Speed FOV")]
    [SerializeField] bool useSpeedFOV = true;
    [SerializeField] float minFOV = 60f;
    [SerializeField] float maxFOV = 85f;
    [SerializeField] float maxSpeedForFOV = 100f;
    [SerializeField] float fovSmoothSpeed = 3f;

    [Header("Camera Shake")]
    [SerializeField] float shakeDuration = 0.5f;
    [SerializeField] float shakeIntensity = 0.15f;
    [SerializeField] float shakeSpeed = 25f;

    Camera cam;
    Rigidbody targetRigidbody;

    Vector3 positionVelocity;
    float currentBank;

    float shakeTimer;
    float currentShakeIntensity;
    float shakeTime;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        FindTarget();

        if (target != null)
        {
            SnapToTarget();
        }
    }

    void LateUpdate()
    {
        // Look for the player if no target exists.
        if (target == null)
        {
            if (autoFindPlayer)
                FindTarget();

            if (target == null)
                return;
        }

        UpdatePosition();
        UpdateRotation();
        UpdateFOV();
    }

    void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
            return;

        SetTarget(player.transform);
    }

    void UpdatePosition()
    {
        // Convert local offset into world position.
        Vector3 desiredPosition = target.TransformPoint(followOffset);

        // Smoothly follow the player.
        Vector3 newPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime,
            maxFollowSpeed
        );

        // Apply camera shake without accumulating offsets.
        transform.position = newPosition + GetShakeOffset();
    }

    void UpdateRotation()
    {
        Vector3 lookPosition = target.position +
                               target.up * lookHeight +
                               target.forward * lookAheadDistance;

        Vector3 direction = lookPosition - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector3 upDirection = useTargetUpDirection
            ? target.up
            : Vector3.up;

        Quaternion desiredRotation = Quaternion.LookRotation(
            direction.normalized,
            upDirection
        );

        // Calculate banking from player rotation.
        if (useBanking && targetRigidbody != null)
        {
            float localYaw = target.InverseTransformDirection(
                targetRigidbody.angularVelocity
            ).y;

            float targetBank = Mathf.Clamp(
                -localYaw * bankAmount,
                -maxBankAngle,
                maxBankAngle
            );

            currentBank = Mathf.Lerp(
                currentBank,
                targetBank,
                bankSmoothSpeed * Time.deltaTime
            );
        }
        else
        {
            currentBank = Mathf.Lerp(
                currentBank,
                0f,
                bankSmoothSpeed * Time.deltaTime
            );
        }

        desiredRotation *= Quaternion.Euler(0f, 0f, currentBank);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    void UpdateFOV()
    {
        if (!useSpeedFOV || targetRigidbody == null)
            return;

        float speed = targetRigidbody.linearVelocity.magnitude;

        float speedPercentage = Mathf.Clamp01(
            speed / Mathf.Max(1f, maxSpeedForFOV)
        );

        float desiredFOV = Mathf.Lerp(
            minFOV,
            maxFOV,
            speedPercentage
        );

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            desiredFOV,
            fovSmoothSpeed * Time.deltaTime
        );
    }

    Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f)
            return Vector3.zero;

        shakeTimer -= Time.deltaTime;
        shakeTime += Time.deltaTime * shakeSpeed;

        float strength = currentShakeIntensity *
            Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));

        float x = Mathf.PerlinNoise(shakeTime, 0f) - 0.5f;
        float y = Mathf.PerlinNoise(0f, shakeTime) - 0.5f;

        return (transform.right * x + transform.up * y) * strength * 2f;
    }

    // Assign a player manually or after respawning.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        targetRigidbody = target != null
            ? target.GetComponent<Rigidbody>()
            : null;

        positionVelocity = Vector3.zero;
    }

    // Immediately position camera behind player.
    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = target.TransformPoint(followOffset);

        Vector3 lookPosition = target.position +
                               target.up * lookHeight +
                               target.forward * lookAheadDistance;

        Vector3 direction = lookPosition - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                direction.normalized,
                useTargetUpDirection ? target.up : Vector3.up
            );
        }

        positionVelocity = Vector3.zero;
    }

    // Trigger camera shake.
    public void Shake()
    {
        Shake(shakeDuration, shakeIntensity);
    }

    public void Shake(float duration, float intensity)
    {
        shakeDuration = duration;
        currentShakeIntensity = intensity;
        shakeTimer = duration;
        shakeTime = 0f;
    }
}
