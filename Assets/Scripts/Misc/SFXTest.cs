using UnityEngine;
using UnityEngine.Android;

public class SFXTest : MonoBehaviour
{
    public AudioClip audioClip;
    void Start()
    {
        SFXManager.instance.PlaySFXClip(audioClip, transform,1);
    }
}
