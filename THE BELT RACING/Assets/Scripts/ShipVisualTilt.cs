
using UnityEngine;

public class ShipVisualTilt : MonoBehaviour
{
    public Transform shipBody;
    public float tiltAngle = 20f;
    public float tiltSpeed = 5f;

    private Quaternion startRotation;
    private PlayerMovement playerMovement;

    private void Start()
    {
        startRotation = shipBody.localRotation;
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        float tiltX = playerMovement.GetSteeringInput().y * tiltAngle;
        float tiltZ = -playerMovement.GetSteeringInput().x * tiltAngle;

        Quaternion targetRotation = startRotation *
            Quaternion.Euler(tiltX, 0f, tiltZ);

        shipBody.localRotation = Quaternion.Slerp(
            shipBody.localRotation,
            targetRotation,
            tiltSpeed * Time.deltaTime
        );
    }
}
