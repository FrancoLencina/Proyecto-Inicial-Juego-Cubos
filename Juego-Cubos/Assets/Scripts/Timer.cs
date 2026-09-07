using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("Game Managers")]

    [SerializeField]
    private GameManager gameManager;

    [SerializeField]
    private LocalGameManager localGameManager;

    [SerializeField]
    private NetworkGameManager multiplayerGameManager;


    [Header("Timer")]

    [SerializeField]
    private float targetTime = 60f;

    [SerializeField]
    private float timeRemaining;

    private bool isCountingDown = false;


    [Header("UI")]

    [SerializeField]
    private TMP_Text textTimer;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        timeRemaining = targetTime;

        isCountingDown = true;

        Debug.Log(timeRemaining);

        UpdateTimerDisplay(timeRemaining);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (!isCountingDown)
            return;

        if (timeRemaining <= 0f)
            return;


        timeRemaining -= Time.deltaTime;


        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;

            UpdateTimerDisplay(timeRemaining);

            TimerStop();

            return;
        }


        UpdateTimerDisplay(timeRemaining);
    }


    // =====================================================
    // DISPLAY
    // =====================================================

    private void UpdateTimerDisplay(
        float timeToDisplay
    )
    {
        if (textTimer == null)
            return;


        int minutes =
            Mathf.FloorToInt(
                timeToDisplay / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                timeToDisplay % 60f
            );


        textTimer.text =
            string.Format(
                "{0:00}:{1:00}",
                minutes,
                seconds
            );
    }


    // =====================================================
    // STOP
    // =====================================================

    public void TimerStop()
    {
        if (!isCountingDown)
            return;


        isCountingDown = false;


        Debug.Log(
            "[Timer] Tiempo terminado."
        );


        // =============================================
        // SINGLEPLAYER
        // =============================================

        if (gameManager != null)
        {
            gameManager.timeRanOut();

            return;
        }


        // =============================================
        // LOCAL MULTIPLAYER
        // =============================================

        if (localGameManager != null)
        {
            localGameManager.TimeRanOut();

            return;
        }

        // =============================================
        // ONLINE MULTIPLAYER
        // =============================================
        
        if (multiplayerGameManager != null)
        {
            multiplayerGameManager.TimeRanOut();
            return;

        }


        Debug.LogWarning(
            "[Timer] No hay ningún GameManager asignado."
        );
    }


    // =====================================================
    // PROPIEDADES
    // =====================================================

    public float TimeRemaining =>
        timeRemaining;

    public bool IsCountingDown =>
        isCountingDown;
}