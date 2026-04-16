using UnityEngine;

public class PlaneSpectate : MonoBehaviour
{
    [Header("Rotation")]
    public float holdRotationSpeed = 180f;
    public float clickRotationAmount = 60f;

    private int holdDirection = 0;

    private void Update()
    {
        if (holdDirection == 0)
        {
            return;
        }

        transform.Rotate(Vector3.up, holdDirection * holdRotationSpeed * Time.deltaTime, Space.World);
    }

    public void RotateLeft()
    {
        transform.Rotate(Vector3.up, -clickRotationAmount, Space.World);
    }

    public void RotateRight()
    {
        transform.Rotate(Vector3.up, clickRotationAmount, Space.World);
    }

    public void StartRotateLeft()
    {
        holdDirection = -1;
    }

    public void StartRotateRight()
    {
        holdDirection = 1;
    }

    public void StopRotate()
    {
        holdDirection = 0;
    }
}
