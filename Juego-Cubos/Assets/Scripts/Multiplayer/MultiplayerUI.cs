using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject createJoinPanel;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button confirmJoinButton;

    [Header("Room")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;

    private bool updatingRoom = false;

    private void Start()
    {
        // Sala oculta al iniciar
        roomPanel.SetActive(false);

        // Elementos de unirse ocultos
        joinCodeInput.gameObject.SetActive(false);
        confirmJoinButton.gameObject.SetActive(false);

        // Eventos
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(ShowJoinMenu);
        confirmJoinButton.onClick.AddListener(JoinRoom);

        leaveRoomButton.onClick.AddListener(LeaveRoom);
        startGameButton.onClick.AddListener(StartGame);

        // Escuchar cuando el Host cierra la sala
        MultiplayerManager.Instance.SessionClosed += OnSessionClosed;
    }

    private void OnDestroy()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.SessionClosed -= OnSessionClosed;
        }
    }

    // =========================
    // CREAR SALA
    // =========================

    private async void CreateRoom()
    {
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        statusText.text = "Creando sala...";

        string code =
            await MultiplayerManager.Instance.CreateRoom();

        if (code != null)
        {
            createJoinPanel.SetActive(false);
            roomPanel.SetActive(true);

            roomCodeText.text = "Código: " + code;

            playersText.text = "Jugadores: 1/2";
            statusText.text = "Esperando otro jugador...";

            // No puede comenzar todavía
            startGameButton.interactable = false;

            StartRoomUpdater();
        }
        else
        {
            statusText.text = "No se pudo crear la sala.";

            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
        }
    }

    // =========================
    // ACTUALIZAR SALA
    // =========================

    private void StartRoomUpdater()
    {
        if (!updatingRoom)
        {
            updatingRoom = true;
            UpdateRoomStatus();
        }
    }

    private async void UpdateRoomStatus()
    {
        while (roomPanel.activeSelf)
        {
            if (!MultiplayerManager.Instance.HasActiveSession())
            {
                break;
            }

            int players =
                MultiplayerManager.Instance.GetPlayerCount();

            playersText.text = $"Jugadores: {players}/2";

            // =========================
            // HOST
            // =========================

            if (MultiplayerManager.Instance.IsHost)
            {
                if (players >= 2)
                {
                    statusText.text = "¡Jugador conectado!";
                    startGameButton.interactable = true;
                }
                else
                {
                    statusText.text = "Esperando otro jugador...";
                    startGameButton.interactable = false;
                }
            }

            // =========================
            // CLIENTE
            // =========================

            else
            {
                statusText.text = "Esperando al anfitrión...";
                startGameButton.interactable = false;
            }

            await Task.Delay(1000);
        }

        updatingRoom = false;
    }

    // =========================
    // HOST CERRÓ LA SALA
    // =========================

    private void OnSessionClosed()
    {
        Debug.Log("El Host cerró la sala.");

        // Detener cualquier actualización
        updatingRoom = false;

        // Mostrar mensaje antes de volver al menú
        statusText.text = "El anfitrión cerró la sala.";

        playersText.text = "";

        // Desconectar Netcode
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        // Volver al menú después de un pequeño momento
        Invoke(nameof(ReturnToMenuAfterHostClosed), 1.5f);
    }

    private void ReturnToMenuAfterHostClosed()
    {
        roomPanel.SetActive(false);
        createJoinPanel.SetActive(true);

        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;

        // Muy importante:
        // volver a habilitar el botón de confirmar
        confirmJoinButton.interactable = true;

        joinCodeInput.gameObject.SetActive(false);
        confirmJoinButton.gameObject.SetActive(false);

        roomCodeText.text = "";
        playersText.text = "";
        statusText.text = "";

        updatingRoom = false;
    }

    // =========================
    // SALIR DE LA SALA
    // =========================

    private async void LeaveRoom()
    {
        statusText.text = "Saliendo...";

        await MultiplayerManager.Instance.LeaveRoom();

        ResetRoomUI();
    }

    // =========================
    // RESET UI
    // =========================

    private void ResetRoomUI()
    {
        roomPanel.SetActive(false);
        createJoinPanel.SetActive(true);

        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;

        joinCodeInput.gameObject.SetActive(false);

        confirmJoinButton.gameObject.SetActive(false);
        confirmJoinButton.interactable = true;

        roomCodeText.text = "";
        playersText.text = "";
        statusText.text = "";

        updatingRoom = false;
    }

    // =========================
    // COMENZAR PARTIDA
    // =========================

    private void StartGame()
    {
        MultiplayerManager.Instance.StartGame();
    }

    // =========================
    // MOSTRAR MENU UNIRSE
    // =========================

    private void ShowJoinMenu()
    {
        joinCodeInput.gameObject.SetActive(true);
        confirmJoinButton.gameObject.SetActive(true);

        confirmJoinButton.interactable = true;

        joinCodeInput.text = "";
        joinCodeInput.Select();
        joinCodeInput.ActivateInputField();
    }

    // =========================
    // UNIRSE A SALA
    // =========================

    private async void JoinRoom()
    {
        string code =
            joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Ingresá un código.");
            return;
        }

        confirmJoinButton.interactable = false;

        statusText.text = "Buscando sala...";

        bool success =
            await MultiplayerManager.Instance.JoinRoom(code);

        if (success)
        {
            createJoinPanel.SetActive(false);
            roomPanel.SetActive(true);

            roomCodeText.text = "Código: " + code;

            playersText.text = "Jugadores: 2/2";
            statusText.text = "Esperando al anfitrión...";

            // El cliente nunca puede iniciar
            startGameButton.interactable = false;

            StartRoomUpdater();
        }
        else
        {
            statusText.text = "No se encontró la sala.";

            // Puede volver a intentar
            confirmJoinButton.interactable = true;
        }
    }
}