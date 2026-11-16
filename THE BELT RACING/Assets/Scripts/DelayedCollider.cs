
using UnityEngine;
using System.Collections;

public class DelayedCollider: MonoBehaviour
{
    [Header("Collider Settings")]
    [Tooltip("The collider that will be enabled after the delay.")]
    [SerializeField] Collider targetCollider;

    [Header("Activation Settings")]
    [Tooltip("How long to wait before enabling the collider.")]
    [Min(0f)]
    [SerializeField] float activationDelay = 0.25f;

    [Tooltip("Disable the collider immediately when this object starts.")]
    [SerializeField] bool disableOnAwake = true;

    [Tooltip("Automatically enable the collider after the delay.")]
    [SerializeField] bool activateOnStart = true;

    private Coroutine activationRoutine;

    private void Awake()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<Collider>();

        if (disableOnAwake && targetCollider != null)
            targetCollider.enabled = false;
    }

    private void Start()
    {
        if (activateOnStart)
            ActivateAfterDelay();
    }

    public void ActivateAfterDelay()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("DelayedCollider: No collider assigned!", this);
            return;
        }

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        activationRoutine = StartCoroutine(ActivationCountdown());
    }

    private IEnumerator ActivationCountdown()
    {
        targetCollider.enabled = false;

        yield return new WaitForSeconds(activationDelay);

        targetCollider.enabled = true;

        activationRoutine = null;
    }

    public void EnableImmediately()
    {
        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        if (targetCollider != null)
            targetCollider.enabled = true;
    }

    public void DisableCollider()
    {
        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        if (targetCollider != null)
            targetCollider.enabled = false;
    }
}
