using UnityEngine;

public class BackToMainTEMP : MonoBehaviour
{
    public void ReturnToMain()
    {
        Loader.Load(Loader.Scene.MainMenu);
    }
    
}
