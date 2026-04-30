using UnityEngine; 
using UnityEngine.InputSystem; 

public class PauseMenu : MenuBase
{
   [SerializeField] private GameObject PauseMenuUICanvas;
   
   // [SerializeField] public InputActionReference ActionToggle; 
   
   private bool isGamePaused = false;

  // Event Functions
   private void Update()
   {
      if (Keyboard.current.escapeKey.wasPressedThisFrame)
      {
         TogglePauseGame();
      }
   }

   // private void Awake()
   // {
   //    ActionToggle.action.performed += ActionToggle_InteractPerformed; 
   // }
   //
   // private void ActionToggle_InteractPerformed(InputAction.CallbackContext obj)
   // {
   //    Debug.Log("THIS WOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOORKS IT WORKS WIT WORKS IT OWKRKKSK");
   //    TogglePauseGame();
   // }

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




