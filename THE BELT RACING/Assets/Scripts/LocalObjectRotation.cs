
using UnityEngine;

public class LocalObjectRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    [SerializeField] bool rotateX = false;
    [SerializeField] bool rotateY = true;
    [SerializeField] bool rotateZ = false;

    [Header("Control")]
    [SerializeField] bool rotateOnStart = true;

    void Update()
    {
        if (!rotateOnStart)
            return;

        Vector3 rotation = new Vector3(
            rotateX ? rotationSpeed.x : 0f,
            rotateY ? rotationSpeed.y : 0f,
            rotateZ ? rotationSpeed.z : 0f
        );

        transform.Rotate(rotation * Time.deltaTime, Space.Self);
    }
}
