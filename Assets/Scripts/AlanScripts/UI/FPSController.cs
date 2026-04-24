using System;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    [SerializeField] private GameObject FPSCounterObject;

    private void Update()
    {
        // Disable or reenable the FPS Counter
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            bool isActive = FPSCounterObject.activeSelf;
            FPSCounterObject.SetActive(!isActive);
        }
    }
}
