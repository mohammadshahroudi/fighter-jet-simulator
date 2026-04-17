using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private GameInput gameInput;
    private void Update()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        float upDown = 0f;

        if (Input.GetKey(KeyCode.Space))
        {
            upDown = 1f;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            upDown = -1f;
        }

        Vector3 moveDir =
            transform.forward * -inputVector.y +
            transform.right * -inputVector.x +
            Vector3.up * upDown;

        moveDir = moveDir.normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}
