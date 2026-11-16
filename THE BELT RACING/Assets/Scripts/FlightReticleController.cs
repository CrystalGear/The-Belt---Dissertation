
using UnityEngine;
using UnityEngine.InputSystem;

public class FlightReticleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform reticle;

    [Header("Mouse Settings")]
    [SerializeField] float mouseSensitivity = 1f;

    [Header("Controller Settings")]
    [SerializeField] float controllerSpeed = 700f;
    [SerializeField] float controllerDeadZone = 0.15f;

    [Header("Reticle Movement")]
    [SerializeField, Range(0.1f, 0.5f)]
    float movementRadius = 0.35f;

    [SerializeField] float returnSpeed = 4f;

    Vector2 reticlePosition;

    Canvas parentCanvas;

    void Awake()
    {
        parentCanvas = reticle.GetComponentInParent<Canvas>();

        reticlePosition = Vector2.zero;
        reticle.anchoredPosition = reticlePosition;
    }

    void Update()
    {
        if (reticle == null)
            return;

        Vector2 mouseInput = Vector2.zero;
        Vector2 controllerInput = Vector2.zero;

        // Mouse input
        if (Mouse.current != null)
        {
            mouseInput = Mouse.current.delta.ReadValue();
        }

        // Controller input
        if (Gamepad.current != null)
        {
            controllerInput = Gamepad.current.leftStick.ReadValue();
        }

        bool hasMouseInput = mouseInput.sqrMagnitude > 0.01f;

        bool hasControllerInput =
            controllerInput.sqrMagnitude >
            controllerDeadZone * controllerDeadZone;

        // Mouse movement
        if (hasMouseInput)
        {
            float canvasScale = parentCanvas != null
                ? parentCanvas.scaleFactor : 1f;

            reticlePosition += mouseInput *
                (mouseSensitivity / canvasScale);
        }
        // Controller movement
        else if (hasControllerInput)
        {
            reticlePosition += controllerInput *
                controllerSpeed * Time.deltaTime;
        }
        // Return reticle towards centre
        else
        {
            float returnAmount =
                1f - Mathf.Exp(-returnSpeed * Time.deltaTime);

            reticlePosition = Vector2.Lerp(
                reticlePosition,
                Vector2.zero,
                returnAmount
            );
        }

        // Calculate movement boundary
        float radius = Screen.height *
            movementRadius /
            (parentCanvas != null ? parentCanvas.scaleFactor : 1f);

        // Clamp reticle to circular boundary
        reticlePosition = Vector2.ClampMagnitude(
            reticlePosition,
            radius
        );

        // Apply position
        reticle.anchoredPosition = reticlePosition;
    }
}
