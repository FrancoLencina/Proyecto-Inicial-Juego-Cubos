using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LocalGameManager : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private List<FruitData> availableFruits;
    [SerializeField] private int sequenceLength = 5;

    [Header("Player 1")]
    [SerializeField] private LocalPlayerMovement player1Movement;
    [SerializeField] private LocalSequenceUI player1SequenceUI;

    [Header("Player 2")]
    [SerializeField] private LocalPlayerMovement player2Movement;
    [SerializeField] private LocalSequenceUI player2SequenceUI;

    [Header("Result")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text resultText;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // =========================================================
    // SEQUENCE
    // =========================================================

    private List<FruitData> targetSequence;

    public IReadOnlyList<FruitData> TargetSequence => targetSequence;

    // =========================================================
    // PLAYER PROGRESS
    // =========================================================

    private int player1Progress = 0;
    private int player2Progress = 0;

    public int Player1Progress => player1Progress;
    public int Player2Progress => player2Progress;

    // =========================================================
    // GAME STATE
    // =========================================================

    private bool gameCompleted = false;
    private bool didTimeRunOut = false;

    private int winningPlayer = 0;

    private bool waitingForPlayerToLand = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Time.timeScale = 1f;

        GenerateSequence();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        // Buscar jugadores automáticamente si no fueron asignados
        FindPlayers();

        // Cursor de gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // =========================================================
    // UPDATE
    // =========================================================
private void Update()
{
   // -----------------------------------------------------
// TEST: P = VICTORIA PLAYER 1
// -----------------------------------------------------

if (!gameCompleted &&
    Keyboard.current != null &&
    Keyboard.current.pKey.wasPressedThisFrame)
{
    Debug.Log(
        "[LocalGameManager] TEST: Victoria Player 1."
    );

    winningPlayer = 1;

    ShowVictory();
}


// -----------------------------------------------------
// TEST: L = VICTORIA PLAYER 2
// -----------------------------------------------------

if (!gameCompleted &&
    Keyboard.current != null &&
    Keyboard.current.lKey.wasPressedThisFrame)
{
    Debug.Log(
        "[LocalGameManager] TEST: Victoria Player 2."
    );

    winningPlayer = 2;

    ShowVictory();
}


// -----------------------------------------------------
// TEST: O = EMPATE
// -----------------------------------------------------

if (!gameCompleted &&
    Keyboard.current != null &&
    Keyboard.current.oKey.wasPressedThisFrame)
{
    Debug.Log(
        "[LocalGameManager] TEST: Empate."
    );

    DrawGame();
}

    // -----------------------------------------------------
    // ESPERAR ATERRIZAJE
    // -----------------------------------------------------

    if (waitingForPlayerToLand)
    {
        CheckWinningPlayerLanding();
        return;
    }

    // -----------------------------------------------------
    // TIEMPO AGOTADO
    // -----------------------------------------------------

    if (!gameCompleted && didTimeRunOut)
    {
        DrawGame();
    }
}
    // =========================================================
    // FIND PLAYERS
    // =========================================================

    private void FindPlayers()
{
    LocalPlayerMovement[] players =
        FindObjectsByType<LocalPlayerMovement>();

    foreach (LocalPlayerMovement player in players)
    {
        if (player.PlayerType ==
            LocalPlayerMovement.PlayerNumber.Player1)
        {
            player1Movement = player;
        }
        else if (player.PlayerType ==
                 LocalPlayerMovement.PlayerNumber.Player2)
        {
            player2Movement = player;
        }
    }

    if (player1Movement == null)
    {
        Debug.LogWarning(
            "[LocalGameManager] Player 1 no encontrado."
        );
    }

    if (player2Movement == null)
    {
        Debug.LogWarning(
            "[LocalGameManager] Player 2 no encontrado."
        );
    }
}

    // =========================================================
    // TIME
    // =========================================================

    public void TimeRanOut()
    {
        if (gameCompleted)
            return;

        didTimeRunOut = true;
    }

    // Mantener compatibilidad si tu Timer llama a timeRanOut()
    public void timeRanOut()
    {
        TimeRanOut();
    }

    // =========================================================
    // GENERATE SEQUENCE
    // =========================================================

    private void GenerateSequence()
    {
        if (availableFruits == null ||
            availableFruits.Count < sequenceLength)
        {
            Debug.LogError(
                "[LocalGameManager] No hay suficientes frutas " +
                "disponibles para generar la secuencia."
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

        Debug.Log(
            "[LocalGameManager] Secuencia generada:"
        );

        for (int i = 0; i < targetSequence.Count; i++)
        {
            Debug.Log(
                i + ": " +
                targetSequence[i].DisplayName
            );
        }

        // =====================================================
        // MISMA SECUENCIA PARA AMBOS JUGADORES
        // =====================================================

        if (player1SequenceUI != null)
        {
            player1SequenceUI.DisplaySequence(
                targetSequence.ToArray()
            );
        }

        if (player2SequenceUI != null)
        {
            player2SequenceUI.DisplaySequence(
                targetSequence.ToArray()
            );
        }
    }

    // =========================================================
    // PLAYER PROGRESS
    // =========================================================

    public void UpdatePlayerProgress(
        int playerNumber,
        int currentIndex
    )
    {
        if (gameCompleted)
            return;

        if (playerNumber == 1)
        {
            player1Progress = currentIndex;

            if (player1SequenceUI != null)
            {
                player1SequenceUI.SetCurrentTarget(
                    currentIndex
                );
            }

            Debug.Log(
                "[LocalGameManager] Player 1 progreso: " +
                player1Progress +
                "/" +
                sequenceLength
            );

            if (player1Progress >= sequenceLength)
            {
                PlayerCompletedSequence(1);
            }
        }
        else if (playerNumber == 2)
        {
            player2Progress = currentIndex;

            if (player2SequenceUI != null)
            {
                player2SequenceUI.SetCurrentTarget(
                    currentIndex
                );
            }

            Debug.Log(
                "[LocalGameManager] Player 2 progreso: " +
                player2Progress +
                "/" +
                sequenceLength
            );

            if (player2Progress >= sequenceLength)
            {
                PlayerCompletedSequence(2);
            }
        }
    }

    // =========================================================
    // PLAYER COMPLETED
    // =========================================================

    public void PlayerCompletedSequence(
        int playerNumber
    )
    {
        if (gameCompleted ||
            waitingForPlayerToLand)
            return;

        if (playerNumber != 1 &&
            playerNumber != 2)
        {
            Debug.LogWarning(
                "[LocalGameManager] Número de jugador inválido: " +
                playerNumber
            );

            return;
        }

        winningPlayer = playerNumber;

        waitingForPlayerToLand = true;

        Debug.Log(
            "[LocalGameManager] Player " +
            winningPlayer +
            " completó la secuencia."
        );

        CheckWinningPlayerLanding();
    }

    // =========================================================
    // CHECK LANDING
    // =========================================================

    private void CheckWinningPlayerLanding()
    {
        if (!waitingForPlayerToLand)
            return;

        LocalPlayerMovement winningMovement = null;

        if (winningPlayer == 1)
            winningMovement = player1Movement;
        else if (winningPlayer == 2)
            winningMovement = player2Movement;

        // Si no encontramos el movimiento,
        // mostramos la victoria igualmente.

        if (winningMovement == null)
        {
            ShowVictory();
            return;
        }

        if (winningMovement.IsGrounded)
        {
            ShowVictory();
        }
    }

    // =========================================================
    // VICTORY
    // =========================================================
private void ShowVictory()
{
    if (gameCompleted)
        return;

    gameCompleted = true;
    waitingForPlayerToLand = false;

    Debug.Log(
        "[LocalGameManager] ¡GANÓ EL JUGADOR " +
        winningPlayer
    );

    SetResultText(
        "¡GANÓ EL JUGADOR " +
        winningPlayer,
        Color.green
    );

    ShowResultPanel();
}

    // =========================================================
    // DEFEAT
    // =========================================================

    private void LoseGame()
    {
        if (gameCompleted)
            return;

        gameCompleted = true;
        waitingForPlayerToLand = false;

        Debug.Log(
            "[LocalGameManager] ¡DERROTA!"
        );

        SetResultText(
            "¡PERDIERON!",
            Color.red
        );

        ShowResultPanel();
    }

    // =========================================================
    // RESULT TEXT
    // =========================================================

    private void SetResultText(
        string text,
        Color color
    )
    {
        if (resultText == null)
        {
            Debug.LogWarning(
                "[LocalGameManager] ResultText no está asignado."
            );

            return;
        }

        resultText.text = text;
        resultText.color = color;

        resultText.fontSize = 40f;
        resultText.fontStyle = FontStyles.Bold;
    }

    // =========================================================
    // RESULT PANEL
    // =========================================================

   private void ShowResultPanel()
{
    Time.timeScale = 0f;

    if (victoryPanel != null)
    {
        victoryPanel.SetActive(true);
    }
    else
    {
        Debug.LogWarning(
            "[LocalGameManager] Victory Panel no está asignado."
        );
    }

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

    // =========================================================
    // RETURN TO MAIN MENU
    // =========================================================

    public void ReturnToMainMenu()
    {
        Debug.Log(
            "[LocalGameManager] Volviendo al menú principal..."
        );

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }

    // =========================================================
// DRAW / EMPATE
// =========================================================

private void DrawGame()
{
    if (gameCompleted)
        return;

    gameCompleted = true;

    waitingForPlayerToLand = false;

    Debug.Log(
        "[LocalGameManager] ¡EMPATE!"
    );

    SetResultText(
        "¡EMPATE! Se acabó el tiempo",
        Color.yellow
    );

    ShowResultPanel();
}
}