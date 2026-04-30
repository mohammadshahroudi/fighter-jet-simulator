using System;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        LoadingScene, 
        MainMenu, 
        Overworld,
        OverworldEnviorment, 
        ShopUI
    }
    
    private static Action onLoaderCallBack; 

    public static void Load(Scene scene)
    {
        onLoaderCallBack = () =>
        {
            SceneManager.LoadScene(scene.ToString());
        };
        
        SceneManager.LoadScene(Scene.LoadingScene.ToString()); 
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
