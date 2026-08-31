using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer: MonoBehaviour {
   
   [SerializeField] private float targetTime = 60.0f;
   [SerializeField] private float timeRemaining;
   private bool isCountingDown = false;
   [SerializeField] private TMP_Text textTimer;

   void Start()
    {
      isCountingDown = true;
      timeRemaining = targetTime;
    }
   void Update()
    {
        if (isCountingDown && timeRemaining > 0) {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining < 0)
               timeRemaining = 0.0f;

               UpdateTimerDisplay(timeRemaining);

            if (timeRemaining == 0) {
               timerStop();
            }
        }
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
         // Pasamos float a INT para que entre en formato MM//SS
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        textTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

   public void timerStop() {
      isCountingDown = false;
      SceneManager.LoadScene("EndScene");
    }
}