using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MenuBase
{
   public void LoadGame()
   {
      Loader.Load(Loader.Scene.Overworld);
   }
   
}
