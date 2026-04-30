using UnityEngine;

public class MenuBase : MonoBehaviour
{
    // General Menu Calls
    public void ReturnToMain()
    {
        Loader.Load(Loader.Scene.MainMenu);   
    }
    public void ToShop()
    {
        Loader.Load(Loader.Scene.ShopUI);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
