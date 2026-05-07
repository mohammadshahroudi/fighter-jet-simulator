using UnityEngine;

public class Up : MonoBehaviour
{
    void Update()
    {
        var vector3 = transform.position;
        vector3.y = vector3.y + 0.1f;
        transform.position = vector3;
    }
}
