using UnityEngine;

public class Bullet : MonoBehaviour
{
    // How long bullets actually fly before despawning
    [SerializeField] private float lifetime = 5f;
    // Need this so that bullets dont hit the player during evasive maneouvers
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
