using UnityEngine; 
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
   [SerializeField] private GameObject PauseMenuUICanvas;
   private bool isGamePaused = false;

   // Event Functions
   private void Update()
   {
      if (Keyboard.current.escapeKey.wasPressedThisFrame)
      {
         TogglePauseGame();
      }
   }

   // Pause Menu Toggles 
   private void TogglePauseGame()
   {
      isGamePaused = !isGamePaused;
      if (isGamePaused)
      {
         Time.timeScale = 0f;
         ShowPauseMenu();
      }
      else
      {
         Time.timeScale = 1; 
         HidePauseMenu();
      }
   }

   private void ShowPauseMenu()
   {
      PauseMenuUICanvas.SetActive(true);
   }
   private void HidePauseMenu()
   {
      PauseMenuUICanvas.SetActive(false);
   }
   
   // Public Methods (Button Methods)
   public void QuitGame()
   {
      Application.Quit(); 
   }

   public void ResumeGame()
   {
      TogglePauseGame();
   }

   public void ReturnToMain()
   {
      Loader.Load(Loader.Scene.MainMenu);
   }
   
   public void ToShop()
   {
      Loader.Load(Loader.Scene.ShopUI);
   }
}




