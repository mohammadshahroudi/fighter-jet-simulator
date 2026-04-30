using UnityEngine;

public class LoaderCallBack : MonoBehaviour
{
    private float timer = 3f; // Duration in seconds

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Loader.LoaderCallBack();
            enabled = false; // Disable this script after the callback
        }
    }
}