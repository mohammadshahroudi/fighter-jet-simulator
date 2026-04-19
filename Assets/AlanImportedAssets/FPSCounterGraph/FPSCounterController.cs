using UnityEngine;
public class FPSCounterController : MonoBehaviour
{
    public GameObject targetObject;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            // Toggle active state
            bool isActive = targetObject.activeSelf;
            targetObject.SetActive(!isActive);
        }
    }
}