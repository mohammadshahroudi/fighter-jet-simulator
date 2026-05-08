using UnityEngine; 
using UnityEngine.InputSystem; 

public class PauseMenu : MenuBase
{
   [SerializeField] private GameObject PauseMenuUICanvas;
   
   // [SerializeField] public InputActionReference ActionToggle; 
   
   public static bool isGamePaused = false;

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
}




