using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Fruit Configuration")]
    [SerializeField] private List<FruitData> availableFruits;

    [SerializeField] private int sequenceLength = 5;

    [Header("Result")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text resultText;

    private List<FruitData> targetSequence;

    private NetworkList<int> networkSequence;

    private bool gameFinished;

    private ulong winnerClientId;

    private Coroutine sequenceUICoroutine;

    public IReadOnlyList<FruitData> TargetSequence =>
        targetSequence;

    public bool GameFinished =>
        gameFinished;

    public ulong WinnerClientId =>
        winnerClientId;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        networkSequence =
            new NetworkList<int>();

            
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    // =====================================================
    // NETWORK SPAWN
    // =====================================================

    public override void OnNetworkSpawn()
    {
        networkSequence.OnListChanged +=
            OnSequenceChanged;

        Debug.Log(
            "[NetworkGameManager] Network Spawn | " +
            "IsServer: " + IsServer +
            " | IsClient: " + IsClient +
            " | IsHost: " + IsHost
        );

        if (IsServer)
        {
            GenerateSequence();
        }
        else
        {
            UpdateLocalSequence();
        }
    }

    // =====================================================
    // NETWORK DESPAWN
    // =====================================================

    public override void OnNetworkDespawn()
    {
        networkSequence.OnListChanged -=
            OnSequenceChanged;

        if (sequenceUICoroutine != null)
        {
            StopCoroutine(
                sequenceUICoroutine
            );

            sequenceUICoroutine = null;
        }
    }

    // =====================================================
    // GENERAR SECUENCIA
    // =====================================================

    private void GenerateSequence()
    {
        if (
            availableFruits == null ||
            availableFruits.Count < sequenceLength
        )
        {
            Debug.LogError(
                "[NetworkGameManager] " +
                "No hay suficientes frutas disponibles."
            );

            return;
        }

        List<FruitData> availablePool =
            new List<FruitData>(
                availableFruits
            );

        networkSequence.Clear();

        for (
            int i = 0;
            i < sequenceLength;
            i++
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    availablePool.Count
                );

            FruitData selectedFruit =
                availablePool[randomIndex];

            int fruitID =
                availableFruits.IndexOf(
                    selectedFruit
                );

            networkSequence.Add(
                fruitID
            );

            availablePool.RemoveAt(
                randomIndex
            );
        }

        Debug.Log(
            "[NetworkGameManager] " +
            "Secuencia generada por el HOST:"
        );

        foreach (
            int fruitID in networkSequence
        )
        {
            Debug.Log(
                "[NetworkGameManager] " +
                availableFruits[fruitID].DisplayName
            );
        }

        UpdateLocalSequence();
    }

    // =====================================================
    // SECUENCIA RECIBIDA
    // =====================================================

    private void OnSequenceChanged(
        NetworkListEvent<int> changeEvent
    )
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "La secuencia cambió. " +
            "Elementos: " +
            networkSequence.Count
        );

        UpdateLocalSequence();
    }

    // =====================================================
    // ACTUALIZAR SECUENCIA LOCAL
    // =====================================================

    private void UpdateLocalSequence()
    {
        if (networkSequence.Count == 0)
            return;

        if (
            availableFruits == null ||
            availableFruits.Count == 0
        )
        {
            Debug.LogError(
                "[NetworkGameManager] " +
                "Available Fruits no está configurado."
            );

            return;
        }

        targetSequence =
            new List<FruitData>();

        foreach (
            int fruitID in networkSequence
        )
        {
            if (
                fruitID < 0 ||
                fruitID >= availableFruits.Count
            )
            {
                Debug.LogError(
                    "[NetworkGameManager] " +
                    "ID de fruta inválido: " +
                    fruitID
                );

                continue;
            }

            targetSequence.Add(
                availableFruits[fruitID]
            );
        }

        Debug.Log(
            "[NetworkGameManager] " +
            "Secuencia local actualizada. " +
            "Cantidad: " +
            targetSequence.Count
        );

        /*
         * La UI es completamente local.
         *
         * Cada jugador muestra la secuencia que recibió
         * en su propio HUD.
         */

        UpdateLocalSequenceUI();
    }

    // =====================================================
    // ACTUALIZAR HUD LOCAL
    // =====================================================

    private void UpdateLocalSequenceUI()
    {
        if (!IsClient)
            return;

        if (
            targetSequence == null ||
            targetSequence.Count == 0
        )
        {
            return;
        }

        if (sequenceUICoroutine != null)
        {
            StopCoroutine(
                sequenceUICoroutine
            );
        }

        sequenceUICoroutine =
            StartCoroutine(
                FindAndDisplaySequenceUI()
            );
    }

    // =====================================================
    // BUSCAR SEQUENCE UI
    // =====================================================

    private IEnumerator FindAndDisplaySequenceUI()
    {
        /*
         * Esperamos algunos frames porque el HUD puede
         * crearse después de que NetworkGameManager
         * reciba la NetworkList.
         */

        SequenceUI sequenceUI = null;

        for (
            int attempt = 0;
            attempt < 30;
            attempt++
        )
        {
            sequenceUI =
                FindAnyObjectByType<SequenceUI>();

            if (sequenceUI != null)
                break;

            yield return null;
        }

        if (sequenceUI == null)
        {
            Debug.LogError(
                "[NetworkGameManager] " +
                "No se encontró SequenceUI en el cliente."
            );

            sequenceUICoroutine = null;

            yield break;
        }

        sequenceUI.DisplaySequence(
            targetSequence.ToArray()
        );

        /*
         * El primer objetivo siempre es el índice 0.
         */

        sequenceUI.SetCurrentTarget(0);

        Debug.Log(
            "[NetworkGameManager] " +
            "Secuencia enviada al HUD local."
        );

        sequenceUICoroutine = null;
    }

    // =====================================================
    // FINALIZAR PARTIDA
    // =====================================================

    public void PlayerCompleted(ulong clientId, bool didHostWin)
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        gameFinished = true;

        winnerClientId =
            clientId;

        Debug.Log(
            "[NetworkGameManager] " +
            "JUGADOR GANADOR: " +
            clientId
        );

        GameFinishedClientRpc(clientId, didHostWin);
    }


    // ====================================================
    // TERMINAR PARTIDA POR EMPATE
    // ====================================================

    public void TimeRanOut(){

        gameFinished = true;

        SetResultText("empate...", Color.black);
        ShowResultPanel();
    }

    // =====================================================
    // COMUNICAR RESULTADO
    // =====================================================

    [ClientRpc]
    private void GameFinishedClientRpc(ulong winningClientId, bool didHostWin)
    {
        if (
            NetworkManager.Singleton == null
        )
        {
            return;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        if (didHostWin)
        {
            Debug.Log(
                "[NetworkGameManager] " +
                "RESULTADO LOCAL: GANASTE"
            );

            OnLocalPlayerWon();
        }
        else
        {
            Debug.Log(
                "[NetworkGameManager] " +
                "RESULTADO LOCAL: PERDISTE"
            );

            OnLocalPlayerLost();
        }
    }

    // =====================================================
    // RESULTADO LOCAL
    // =====================================================

    private void OnLocalPlayerWon()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "El jugador local ganó la partida."
        );

        SoundManager.Instance.PlayWin();

        SetResultText("Jugador 1 ha ganado!", Color.green);
        ShowResultPanel();
    }

    private void OnLocalPlayerLost()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "El jugador local perdió la partida."
        );

        SoundManager.Instance.PlayLose();

        SetResultText("Jugador 2 ha ganado!", Color.green);
        ShowResultPanel();


    }

    // ======================
    // PANEL DE RESULTADOS
    // ======================

      private void ShowResultPanel()
    {
        // Detener gameplay
        //Time.timeScale = 0f;


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

        public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        // Apagar Network Manager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

        if (NetworkManager.Singleton.gameObject != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        SceneManager.LoadScene("MainMenuScene");
    }
}