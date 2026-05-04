using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MenuBase
{
   public void LoadGame()
   {
      // Uncomment out sections as needed. 
      
      Loader.Load(Loader.Scene.Overworld);
      // Loader.Load(Loader.Scene.OverworldEnviorment);

   }
   
}
