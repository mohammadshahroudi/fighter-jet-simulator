using UnityEngine;

public class GameTimer : MonoBehaviour
{
   private static float timeLeft = 30f;
   private bool timerActive = true;
   private static float timerCounter;
   // float seconds;

   public void StartTimer()
   {
      timerActive = true;
   }

   public void StopTimer()
   {
      timerActive = false;
   }
   
   private void Update()
   {
      if (timerActive)
      {
         timeLeft -= Time.deltaTime;
         Debug.Log("Time left: " + (int) timeLeft);
         
         if (timeLeft <= 0)
         {
            Debug.Log("Game Over!");
            StopTimer();
         }

         // This is supposed to add about 30 seconds if the player 
         // kills an enemy
         if (timeLeft <= 20)
         {
            timeLeft += 30f;
         }
         
         // This is supposed to be if the player 
         // survives for about 300 seconds which is equivalent
         // to 5 minutes then
         // spawn the boss
      }

      
   }
}

/*// Starts at 30 seconds
timeLeft -= Time.deltaTime;
seconds = Mathf.FloorToInt(timeLeft % 60);
Debug.Log("Remaining Time: " + seconds);

if (seconds <= 15)
{
   // Adds about 30 seconds
   timeLeft += 30;
   seconds = Mathf.FloorToInt(timeLeft % 60);
   Debug.Log("Remaining Time: " + seconds);
}*/