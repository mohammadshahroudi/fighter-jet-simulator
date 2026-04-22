using System;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Rhu_LoadingScene, 
        Rhu_MainMenu, 
        Rhu_SampleScene, 
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
        
        SceneManager.LoadScene(Scene.Rhu_LoadingScene.ToString()); 
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
