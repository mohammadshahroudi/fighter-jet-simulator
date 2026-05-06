using UnityEngine;

public class GameTimer : MonoBehaviour
{
   private static float timeLeft = 10f;
   private bool timerActive = true;
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
      }

      if (timeLeft <= 0)
      {
         Debug.Log("Game Over!");
         StopTimer();
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