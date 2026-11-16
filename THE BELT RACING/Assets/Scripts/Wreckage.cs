
using UnityEngine;

public class Wreckage: MonoBehaviour
{
    [Header("Wreckage Pieces")]
    [SerializeField] Rigidbody[] wreckagePieces;

    [Header("Explosion Force")]
    [SerializeField] float minVelocityForce = 5f;
    [SerializeField] float maxVelocityForce = 15f;

    [Header("Rotational Force")]
    [SerializeField] float minTorqueForce = 5f;
    [SerializeField] float maxTorqueForce = 20f;

    [Header("Force Settings")]
    [SerializeField] ForceMode forceMode = ForceMode.Impulse;

    void Start()
    {
        ApplyWreckageForces();
    }

    void ApplyWreckageForces()
    {
        foreach (Rigidbody piece in wreckagePieces)
        {
            if (piece == null)
                continue;

            // Random direction
            Vector3 randomDirection = Random.onUnitSphere;

            // Random velocity force
            float velocityForce = Random.Range(minVelocityForce, maxVelocityForce);

            piece.AddForce(randomDirection * velocityForce, forceMode);

            // Random rotational direction and force
            Vector3 randomTorque = Random.insideUnitSphere;

            float torqueForce = Random.Range(minTorqueForce, maxTorqueForce);

            piece.AddTorque(randomTorque * torqueForce, forceMode);
        }
    }
}
