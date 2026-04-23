using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
   public void QuitGame() 
   {
      Application.Quit();
      
      Debug.Log("Game has been quit."); 
   }

   public void LoadGame()
   {
      Loader.Load(Loader.Scene.HectorTest);
   }
   
   public void ToShop()
   {
      Loader.Load(Loader.Scene.ShopUI);
   }
   
}
