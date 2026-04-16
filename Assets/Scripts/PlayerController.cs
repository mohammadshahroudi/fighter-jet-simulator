using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private GameInput gameInput;
    private void Update()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        
        float moveAmount = -inputVector.y;
        float turnAmount = -inputVector.x;

        transform.position += transform.forward * moveAmount * moveSpeed * Time.deltaTime;
        transform.Rotate(0f, turnAmount * rotateSpeed * Time.deltaTime, 0f);
        //Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        //transform.position += moveDir * moveSpeed * Time.deltaTime;
        //float rotateSpeed = 10f;
        //transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }
}
