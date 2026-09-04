using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private List<FruitData> availableFruits;
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private SequenceUI sequenceUI;

    [Header("Timer")]
    [SerializeField] private float gameTime = 60f;

    [Header("Result")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCamera playerCamera;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private List<FruitData> targetSequence;

    private float remainingTime;

    private bool gameCompleted = false;
    private bool waitingForPlayerToLand = false;

    public IReadOnlyList<FruitData> TargetSequence => targetSequence;

    private void Start()
    {
        Time.timeScale = 1f;

        GenerateSequence();

        remainingTime = gameTime;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        // Buscar referencias automáticamente si no fueron asignadas
        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerCamera == null)
            playerCamera = FindAnyObjectByType<PlayerCamera>();

        // Cursor de gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // TEST: P = probar victoria
    if (!gameCompleted &&
        Keyboard.current != null &&
        Keyboard.current.pKey.wasPressedThisFrame)
    {
        Debug.Log("[GameManager] TEST: Victoria activada con P.");

        CompleteGame();
    }

    // TEST: O = probar derrota
    if (!gameCompleted &&
        Keyboard.current != null &&
        Keyboard.current.oKey.wasPressedThisFrame)
    {
        Debug.Log("[GameManager] TEST: Derrota activada con O.");

        remainingTime = 0f;
    }

    if (gameCompleted)
    {
        CheckPlayerLanding();
        return;
    }

    UpdateTimer();
    }

    private void UpdateTimer()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            Debug.Log("[GameManager] ¡Se terminó el tiempo!");

            LoseGame();
        }
    }

    private void GenerateSequence()
    {
        if (availableFruits == null ||
            availableFruits.Count < sequenceLength)
        {
            Debug.LogError(
                "No hay suficientes frutas disponibles para generar la secuencia."
            );

            return;
        }

        targetSequence = new List<FruitData>();

        List<FruitData> availablePool =
            new List<FruitData>(availableFruits);

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex =
                Random.Range(0, availablePool.Count);

            FruitData selectedFruit =
                availablePool[randomIndex];

            targetSequence.Add(selectedFruit);

            availablePool.RemoveAt(randomIndex);
        }

        Debug.Log("Secuencia generada:");

        for (int i = 0; i < targetSequence.Count; i++)
        {
            Debug.Log(
                i + ": " +
                targetSequence[i].DisplayName
            );
        }

        if (sequenceUI != null)
        {
            sequenceUI.DisplaySequence(
                targetSequence.ToArray()
            );
        }
    }

    public void UpdateCurrentTarget(int currentIndex)
    {
        if (sequenceUI == null)
            return;

        sequenceUI.SetCurrentTarget(currentIndex);
    }

    // =========================================================
    // VICTORIA
    // =========================================================

    public void CompleteGame()
    {
        if (gameCompleted || waitingForPlayerToLand)
            return;

        waitingForPlayerToLand = true;

        Debug.Log(
            "[GameManager] Secuencia completada. Esperando a que el jugador aterrice..."
        );

        CheckPlayerLanding();
    }

    private void CheckPlayerLanding()
    {
        if (!waitingForPlayerToLand)
            return;

        if (playerMovement == null)
        {
            ShowVictory();
            return;
        }

        if (playerMovement.IsGrounded)
        {
            ShowVictory();
        }
    }

    private void ShowVictory()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;
        waitingForPlayerToLand = false;

        Debug.Log("[GameManager] ¡VICTORIA!");

        SetResultText(
            "¡GANASTE!",
            Color.green
        );

        ShowResultPanel();
    }

    // =========================================================
    // DERROTA
    // =========================================================

    private void LoseGame()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;
        waitingForPlayerToLand = false;

        Debug.Log("[GameManager] ¡DERROTA!");

        SetResultText(
            "¡PERDISTE!",
            Color.red
        );

        ShowResultPanel();
    }

    // =========================================================
    // PANEL DE RESULTADO
    // =========================================================

    private void SetResultText(
        string text,
        Color color
    )
    {
        if (resultText == null)
        {
            Debug.LogWarning(
                "[GameManager] ResultText no está asignado."
            );

            return;
        }

        resultText.text = text;
        resultText.color = color;

        // Mantener las características visuales
        resultText.fontSize = 40f;
        resultText.fontStyle = FontStyles.Bold;
    }

    private void ShowResultPanel()
    {
        // Detener gameplay
        Time.timeScale = 0f;

        // Desactivar controles de cámara
        if (playerCamera != null)
        {
            playerCamera.SetControlsEnabled(false);
        }

        // Mostrar panel
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[GameManager] Victory Panel no está asignado."
            );
        }

        // Mostrar y liberar mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================================================
    // VOLVER AL MENÚ
    // =========================================================

    public void ReturnToMainMenu()
    {
        Debug.Log("[GameManager] Volviendo al menú principal...");

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}