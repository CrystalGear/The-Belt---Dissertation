
using UnityEngine;

public class UIConnectingLine: MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform target;
    [SerializeField] RectTransform canvasRect;

    [Header("Line Settings")]
    [SerializeField] float lineWidth = 3f;

    RectTransform lineRect;

    void Awake()
    {
        lineRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        UpdateLine();
    }

    void UpdateLine()
    {
        // Get the centre of the screen in canvas space.
        Vector2 startPosition = canvasRect.rect.center;

        // Get the target's centre in canvas space.
        Vector2 targetPosition =
            canvasRect.InverseTransformPoint(
                target.TransformPoint(target.rect.center)
            );

        // Calculate direction and distance.
        Vector2 direction = targetPosition - startPosition;
        float distance = direction.magnitude;

        // Position the line at the centre of the screen.
        lineRect.anchoredPosition = startPosition;

        // Rotate towards the target.
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        // Stretch the line to reach the target.
        lineRect.sizeDelta = new Vector2(distance, lineWidth);
    }
}
