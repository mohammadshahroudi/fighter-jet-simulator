using UnityEngine;

public class Up : MonoBehaviour
{
    void Update()
    {
        var vector3 = transform.position;
        vector3.y = vector3.y + 0.5f;
        transform.position = vector3;

        if (transform.position.y > 170)
        {
            vector3.y = -20f; 
            transform.position = vector3;
        }
    }
}
