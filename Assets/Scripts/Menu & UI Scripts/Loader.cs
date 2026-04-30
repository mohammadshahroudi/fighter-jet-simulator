using System;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        LoadingScreen, 
        MainMenu, 
        HectorTest,
        Overworld,
        ShopUI
    }
    
    private static Action onLoaderCallBack; 

    public static void Load(Scene scene)
    {
        onLoaderCallBack = () =>
        {
            SceneManager.LoadScene(scene.ToString());
        };
        
        SceneManager.LoadScene(Scene.LoadingScreen.ToString()); 
    }

    public static void LoaderCallBack()
    {
        if (onLoaderCallBack != null) 
        {
            onLoaderCallBack(); 
            onLoaderCallBack = null; 
        }
    }
}
