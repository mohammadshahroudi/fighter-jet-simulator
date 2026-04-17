using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float collisionDelay = 0.5f;

    private bool canCollide = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
        Invoke(nameof(EnableCollision), collisionDelay);
    }

    void EnableCollision()
    {
        canCollide = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (canCollide)
        {
            Destroy(gameObject);
        }
    }
}
