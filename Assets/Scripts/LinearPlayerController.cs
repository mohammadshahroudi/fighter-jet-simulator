using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private float YawAmount = 120;
    private float Yaw;

    void Start()
    {
        
    }
    private void Update()
    {
        transform.position -= transform.forward * moveSpeed * Time.deltaTime;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        Yaw += horizontalInput * YawAmount * Time.deltaTime;
        float pitch = Mathf.Lerp(0, 20, Mathf.Abs(verticalInput)) * Mathf.Sign(verticalInput);
        float roll = Mathf.Lerp(0, 30, Mathf.Abs(horizontalInput)) * -Mathf.Sign(horizontalInput);

        transform.localRotation = Quaternion.Euler(Vector3.up * Yaw + Vector3.right * pitch + Vector3.forward * roll);
    }
}
