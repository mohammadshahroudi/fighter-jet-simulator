using System;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
<<<<<<< HEAD
        Rhu_LoadingScene, 
        Rhu_MainMenu, 
        Rhu_SampleScene, 
=======
        LoadingScene, 
        MainMenu, 
>>>>>>> main
        HectorTest,
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
