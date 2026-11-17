
using System.Collections;
using UnityEngine;

public class DestroyAfterDelay: MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] float waitTime = 2f;
    [SerializeField] float shrinkDuration = 1f;

    private void Start()
    {
        StartCoroutine(ShrinkAndDestroy());
    }

    private IEnumerator ShrinkAndDestroy()
    {
        // Wait before shrinking
        yield return new WaitForSeconds(waitTime);

        // Store the starting scale
        Vector3 startScale = transform.localScale;

        float timer = 0f;

        // Smoothly shrink the object
        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;

            float progress = timer / shrinkDuration;

            transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                progress
            );

            yield return null;
        }

        // Ensure scale reaches zero
        transform.localScale = Vector3.zero;

        // Destroy the object
        Destroy(gameObject);
    }
}
