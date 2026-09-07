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

    [Header("Victory Check")]
    [Tooltip("Tiempo que se espera después de completar la pila antes de confirmar la victoria.")]
    [SerializeField] private float victoryCheckDelay = 1f;

    [Header("Result")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text resultText;

    private List<FruitData> targetSequence;

    private NetworkList<int> networkSequence;

    private bool gameFinished;

    private ulong winnerClientId;

    private Coroutine sequenceUICoroutine;

    private Coroutine victoryCoroutine;

    private bool checkingVictory;

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
            StopCoroutine(sequenceUICoroutine);
            sequenceUICoroutine = null;
        }

        if (victoryCoroutine != null)
        {
            StopCoroutine(victoryCoroutine);
            victoryCoroutine = null;
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

        sequenceUI.SetCurrentTarget(0);

        Debug.Log(
            "[NetworkGameManager] " +
            "Secuencia enviada al HUD local."
        );

        sequenceUICoroutine = null;
    }


    // =====================================================
    // JUGADOR COMPLETÓ LA SECUENCIA
    // =====================================================

    public void PlayerCompleted(
        ulong clientId
    )
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        /*
         * Si ya estamos comprobando una victoria,
         * no iniciar otra coroutine.
         */
        if (checkingVictory)
            return;

        Debug.Log(
            "[NetworkGameManager] " +
            "Jugador " +
            clientId +
            " completó la pila."
        );

        checkingVictory = true;

        victoryCoroutine =
            StartCoroutine(
                CheckVictoryAfterDelay(
                    clientId
                )
            );
    }


    // =====================================================
    // ESPERAR Y VOLVER A COMPROBAR
    // =====================================================

    private IEnumerator CheckVictoryAfterDelay(
        ulong completingClientId
    )
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "Esperando " +
            victoryCheckDelay +
            " segundos antes de confirmar la victoria..."
        );

        yield return new WaitForSeconds(
            victoryCheckDelay
        );

        if (gameFinished)
        {
            checkingVictory = false;
            victoryCoroutine = null;
            yield break;
        }

        Debug.Log(
            "[NetworkGameManager] " +
            "Revisando nuevamente las pilas..."
        );

       NetworkTargetZone[] zones =
    FindObjectsByType<NetworkTargetZone>();
        NetworkTargetZone hostZone = null;
        NetworkTargetZone clientZone = null;

        foreach (
            NetworkTargetZone zone in zones
        )
        {
            if (zone == null)
                continue;

            if (zone.IsHostZone)
                hostZone = zone;
            else
                clientZone = zone;
        }

        bool hostComplete =
            hostZone != null &&
            hostZone.IsStackComplete();

        bool clientComplete =
            clientZone != null &&
            clientZone.IsStackComplete();

        Debug.Log(
            "[NetworkGameManager] " +
            "Resultado de la segunda comprobación | " +
            "Host: " +
            hostComplete +
            " | Cliente: " +
            clientComplete
        );

        /*
         * =================================================
         * CASO 1: AMBOS COMPLETARON
         * =================================================
         */

        if (
            hostComplete &&
            clientComplete
        )
        {
            checkingVictory = false;
            victoryCoroutine = null;

            Debug.Log(
                "[NetworkGameManager] " +
                "AMBOS JUGADORES COMPLETARON LA PILA. EMPATE."
            );

            DrawGame();

            yield break;
        }

        /*
         * =================================================
         * CASO 2: EL JUGADOR QUE COMPLETÓ SIGUE COMPLETO
         * =================================================
         */

        bool completingPlayerStillComplete =
            false;

        if (
            completingClientId ==
            NetworkManager.ServerClientId
        )
        {
            completingPlayerStillComplete =
                hostComplete;
        }
        else
        {
            completingPlayerStillComplete =
                clientComplete;
        }

        if (completingPlayerStillComplete)
        {
            checkingVictory = false;
            victoryCoroutine = null;

            Debug.Log(
                "[NetworkGameManager] " +
                "La pila sigue completa. " +
                "Jugador ganador: " +
                completingClientId
            );

            FinishGame(
                completingClientId
            );

            yield break;
        }

        /*
         * =================================================
         * CASO 3: LA PILA SE DESARMÓ
         * =================================================
         */

        Debug.Log(
            "[NetworkGameManager] " +
            "La pila se desarmó durante la espera. " +
            "La partida continúa."
        );

        if (
            hostZone != null &&
            !hostComplete
        )
        {
            hostZone.ResetCompletionState();
        }

        if (
            clientZone != null &&
            !clientComplete
        )
        {
            clientZone.ResetCompletionState();
        }

        checkingVictory = false;
        victoryCoroutine = null;
    }


    // =====================================================
    // FINALIZAR PARTIDA
    // =====================================================

    private void FinishGame(
        ulong winningClientId
    )
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        gameFinished = true;

        winnerClientId =
            winningClientId;

        Debug.Log(
            "[NetworkGameManager] " +
            "PARTIDA FINALIZADA | Ganador ClientId: " +
            winningClientId
        );

        GameFinishedClientRpc(
            winningClientId
        );
    }


    // =====================================================
    // TERMINAR PARTIDA POR EMPATE
    // =====================================================

    public void TimeRanOut()
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        DrawGame();
    }


    private void DrawGame()
    {
        if (gameFinished)
            return;

        gameFinished = true;

        Debug.Log(
            "[NetworkGameManager] " +
            "PARTIDA FINALIZADA | EMPATE"
        );

        DrawGameClientRpc();
    }


    // =====================================================
    // COMUNICAR VICTORIA
    // =====================================================

    [ClientRpc]
    private void GameFinishedClientRpc(
        ulong winningClientId
    )
    {
        if (
            NetworkManager.Singleton == null
        )
        {
            return;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        bool localPlayerWon =
            localClientId ==
            winningClientId;

        if (localPlayerWon)
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
    // COMUNICAR EMPATE
    // =====================================================

    [ClientRpc]
    private void DrawGameClientRpc()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "RESULTADO LOCAL: EMPATE"
        );

        OnLocalPlayerDraw();
    }


    // =====================================================
    // RESULTADO LOCAL - GANADOR
    // =====================================================

    private void OnLocalPlayerWon()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "El jugador local ganó la partida."
        );

        if (
            SoundManager.Instance != null
        )
        {
            SoundManager.Instance.PlayWin();
        }

        SetResultText(
            "¡GANASTE!",
            Color.green
        );

        FreezeLocalPlayer();

        ShowResultPanel();
    }


    // =====================================================
    // RESULTADO LOCAL - PERDEDOR
    // =====================================================

    private void OnLocalPlayerLost()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "El jugador local perdió la partida."
        );

        if (
            SoundManager.Instance != null
        )
        {
            SoundManager.Instance.PlayLose();
        }

        SetResultText(
            "¡PERDISTE!",
            Color.red
        );

        FreezeLocalPlayer();

        ShowResultPanel();
    }


    // =====================================================
    // RESULTADO LOCAL - EMPATE
    // =====================================================

    private void OnLocalPlayerDraw()
    {
        Debug.Log(
            "[NetworkGameManager] " +
            "El jugador local terminó en empate."
        );

        SetResultText(
            "¡EMPATE!",
            Color.black
        );

        FreezeLocalPlayer();

        ShowResultPanel();
    }


    // =====================================================
    // CONGELAR JUGADOR LOCAL
    // =====================================================

    private void FreezeLocalPlayer()
    {
        /*
         * Movimiento
         */

        NetworkPlayerMovement movement =
            FindAnyObjectByType<NetworkPlayerMovement>();

        if (movement != null)
        {
            movement.FreezeForGameEnd();
        }

        /*
         * Interacción
         */

        NetworkPlayerInteraction interaction =
            FindAnyObjectByType<NetworkPlayerInteraction>();

        if (interaction != null)
        {
            interaction.enabled = false;
        }

        /*
         * Cámara
         */

        PlayerCamera playerCamera =
            FindAnyObjectByType<PlayerCamera>();

        if (playerCamera != null)
        {
            playerCamera.enabled = false;
        }
    }


    // =====================================================
    // PANEL DE RESULTADOS
    // =====================================================

    private void ShowResultPanel()
    {
        /*
         * Congelamos solamente el cliente local.
         *
         * No hacerlo en el servidor directamente porque
         * Time.timeScale no se sincroniza por Netcode.
         */

        Time.timeScale = 0f;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[NetworkGameManager] " +
                "Victory Panel no está asignado."
            );
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }


    // =====================================================
    // TEXTO RESULTADO
    // =====================================================

    private void SetResultText(
        string text,
        Color color
    )
    {
        if (resultText == null)
        {
            Debug.LogWarning(
                "[NetworkGameManager] " +
                "ResultText no está asignado."
            );

            return;
        }

        resultText.text = text;
        resultText.color = color;

        resultText.fontSize = 40f;
        resultText.fontStyle = FontStyles.Bold;
    }


    // =====================================================
    // VOLVER AL MENÚ
    // =====================================================

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

            if (
                NetworkManager.Singleton.gameObject != null
            )
            {
                Destroy(
                    NetworkManager.Singleton.gameObject
                );
            }
        }

        SceneManager.LoadScene(
            "MainMenuScene"
        );
    }
}
