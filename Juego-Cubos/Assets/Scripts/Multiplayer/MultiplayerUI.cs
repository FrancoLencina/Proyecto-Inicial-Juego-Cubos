using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MultiplayerUI : MonoBehaviour
{
    // =========================================================
    // MENU
    // =========================================================

    [Header("Menu")]
    [SerializeField] private GameObject createJoinPanel;

    [SerializeField] private Button createRoomButton;

    [SerializeField] private Button joinRoomButton;

    [SerializeField] private TMP_InputField joinCodeInput;

    [SerializeField] private Button confirmJoinButton;

    [SerializeField] private Button returnMenuButton;

    // =========================================================
    // ERRORES
    // =========================================================

    [Header("Join Errors")]
    [SerializeField] private TMP_Text invalidCodeText;

    // =========================================================
    // ROOM
    // =========================================================

    [Header("Room")]
    [SerializeField] private GameObject roomPanel;

    [SerializeField] private TMP_Text roomCodeText;

    [SerializeField] private TMP_Text playersText;

    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button startGameButton;

    [SerializeField] private Button leaveRoomButton;

    // =========================================================
    // ESTADO
    // =========================================================

    private bool updatingRoom = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Debug.Log(
            "[MultiplayerUI] Awake ejecutado."
        );

        RegisterButtonListeners();
    }

    private void Start()
    {
        Debug.Log(
            "[MultiplayerUI] UI inicializada correctamente."
        );

        InitializeUI();

        // -----------------------------------------------------
        // SUSCRIBIRSE AL MANAGER PERSISTENTE
        // -----------------------------------------------------

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.SessionClosed +=
                OnSessionClosed;
        }
        else
        {
            Debug.LogError(
                "[MultiplayerUI] No existe MultiplayerManager."
            );
        }
    }

    private void OnDestroy()
    {
        updatingRoom = false;

        CancelInvoke();

        UnregisterButtonListeners();

        // -----------------------------------------------------
        // DESUSCRIBIRSE DEL MANAGER
        // -----------------------------------------------------

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.SessionClosed -=
                OnSessionClosed;
        }

        Debug.Log(
            "[MultiplayerUI] UI destruida."
        );
    }

    // =========================================================
    // REGISTRAR BOTONES
    // =========================================================

    private void RegisterButtonListeners()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(CreateRoom);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(ShowJoinMenu);
        }

        if (confirmJoinButton != null)
        {
            confirmJoinButton.onClick.AddListener(JoinRoom);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        if (returnMenuButton != null)
        {
            Debug.Log(
                "[MultiplayerUI] Registrando listener de ReturnMenuButton."
            );

            returnMenuButton.onClick.AddListener(
                ReturnToMainMenu
            );
        }
        else
        {
            Debug.LogError(
                "[MultiplayerUI] ReturnMenuButton NO está asignado."
            );
        }

        if (joinCodeInput != null)
        {
            joinCodeInput.onValueChanged.AddListener(
                OnJoinCodeChanged
            );
        }
    }

    // =========================================================
    // DESREGISTRAR BOTONES
    // =========================================================

    private void UnregisterButtonListeners()
    {
        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.RemoveListener(ShowJoinMenu);
        }

        if (confirmJoinButton != null)
        {
            confirmJoinButton.onClick.RemoveListener(JoinRoom);
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.RemoveListener(LeaveRoom);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        if (returnMenuButton != null)
        {
            returnMenuButton.onClick.RemoveListener(
                ReturnToMainMenu
            );
        }

        if (joinCodeInput != null)
        {
            joinCodeInput.onValueChanged.RemoveListener(
                OnJoinCodeChanged
            );
        }
    }

    // =========================================================
    // INICIALIZAR UI
    // =========================================================

    private void InitializeUI()
    {
        updatingRoom = false;

        if (roomPanel != null)
            roomPanel.SetActive(false);

        if (createJoinPanel != null)
            createJoinPanel.SetActive(true);

        if (joinCodeInput != null)
            joinCodeInput.gameObject.SetActive(false);

        if (confirmJoinButton != null)
            confirmJoinButton.gameObject.SetActive(false);

        if (invalidCodeText != null)
            invalidCodeText.gameObject.SetActive(false);

        if (createRoomButton != null)
            createRoomButton.interactable = true;

        if (joinRoomButton != null)
            joinRoomButton.interactable = true;

        if (startGameButton != null)
            startGameButton.interactable = false;
    }

    // =========================================================
    // CREAR SALA
    // =========================================================

    private async void CreateRoom()
    {
        Debug.Log(
            "[MultiplayerUI] CreateRoom presionado."
        );

        if (MultiplayerManager.Instance == null)
        {
            Debug.LogError(
                "[MultiplayerUI] No existe MultiplayerManager."
            );

            return;
        }

        // -----------------------------------------------------
        // DESHABILITAR BOTONES
        // -----------------------------------------------------

        if (createRoomButton != null)
            createRoomButton.interactable = false;

        if (joinRoomButton != null)
            joinRoomButton.interactable = false;

        if (statusText != null)
            statusText.text = "Creando sala...";

        // -----------------------------------------------------
        // PEDIR AL MANAGER QUE CREE LA SALA
        // -----------------------------------------------------

        string code =
            await MultiplayerManager.Instance.CreateRoom();

        // La UI puede haber sido destruida durante el await.
        if (this == null || !gameObject)
            return;

        // -----------------------------------------------------
        // SALA CREADA
        // -----------------------------------------------------

        if (code != null)
        {
            ShowRoomPanel();

            if (roomCodeText != null)
                roomCodeText.text = "Código: " + code;

            if (playersText != null)
                playersText.text = "Jugadores: 1/2";

            if (statusText != null)
                statusText.text =
                    "Esperando otro jugador...";

            if (startGameButton != null)
                startGameButton.interactable = false;

            StartRoomUpdater();
        }
        else
        {
            // -------------------------------------------------
            // ERROR
            // -------------------------------------------------

            if (statusText != null)
                statusText.text =
                    "No se pudo crear la sala.";

            if (createRoomButton != null)
                createRoomButton.interactable = true;

            if (joinRoomButton != null)
                joinRoomButton.interactable = true;
        }
    }

    // =========================================================
    // MOSTRAR ROOM
    // =========================================================

    private void ShowRoomPanel()
    {
        if (createJoinPanel != null)
            createJoinPanel.SetActive(false);

        if (roomPanel != null)
            roomPanel.SetActive(true);
    }

    // =========================================================
    // ACTUALIZAR ESTADO DE LA SALA
    // =========================================================

    private void StartRoomUpdater()
    {
        if (updatingRoom)
            return;

        updatingRoom = true;

        UpdateRoomStatus();
    }

    private async void UpdateRoomStatus()
    {
        while (updatingRoom)
        {
            await Task.Delay(1000);

            if (this == null || !gameObject)
                return;

            if (MultiplayerManager.Instance == null)
            {
                updatingRoom = false;
                return;
            }

            if (roomPanel == null ||
                playersText == null ||
                statusText == null ||
                startGameButton == null)
            {
                updatingRoom = false;
                return;
            }

            // -------------------------------------------------
            // YA NO EXISTE LA SESIÓN
            // -------------------------------------------------

            if (!MultiplayerManager.Instance.HasActiveSession())
            {
                updatingRoom = false;
                return;
            }

            // -------------------------------------------------
            // CANTIDAD DE JUGADORES
            // -------------------------------------------------

            int players =
                MultiplayerManager.Instance.GetPlayerCount();

            playersText.text =
                $"Jugadores: {players}/2";

            // -------------------------------------------------
            // HOST
            // -------------------------------------------------

            if (MultiplayerManager.Instance.IsHost)
            {
                if (players >= 2)
                {
                    statusText.text =
                        "¡Jugador conectado!";

                    startGameButton.interactable = true;
                }
                else
                {
                    statusText.text =
                        "Esperando otro jugador...";

                    startGameButton.interactable = false;
                }
            }

            // -------------------------------------------------
            // CLIENTE
            // -------------------------------------------------

            else
            {
                statusText.text =
                    "Esperando al anfitrión...";

                startGameButton.interactable = false;
            }
        }

        updatingRoom = false;
    }

    // =========================================================
    // SESIÓN CERRADA POR EL HOST
    // =========================================================

    private void OnSessionClosed()
    {
        Debug.Log(
            "[MultiplayerUI] El Host cerró la sala."
        );

        updatingRoom = false;

        if (this == null || !gameObject)
            return;

        if (statusText != null)
            statusText.text =
                "El anfitrión cerró la sala.";

        if (playersText != null)
            playersText.text = "";

        // El Manager ya se encarga de apagar el NetworkManager.
        // La UI solamente actualiza visualmente el estado.

        Invoke(
            nameof(ReturnToMenuAfterHostClosed),
            1.5f
        );
    }

    private void ReturnToMenuAfterHostClosed()
    {
        if (this == null || !gameObject)
            return;

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    // =========================================================
    // SALIR DE LA SALA
    // =========================================================

    private async void LeaveRoom()
    {
        Debug.Log(
            "[MultiplayerUI] LeaveRoom presionado."
        );

        if (MultiplayerManager.Instance == null)
        {
            ResetRoomUI();
            return;
        }

        updatingRoom = false;

        if (statusText != null)
            statusText.text = "Saliendo...";

        // -----------------------------------------------------
        // EL MANAGER SE ENCARGA DE LA SESIÓN
        // -----------------------------------------------------

        await MultiplayerManager.Instance.LeaveRoom();

        if (this == null || !gameObject)
            return;

        // -----------------------------------------------------
        // LA UI SOLO RESTABLECE SU ESTADO
        // -----------------------------------------------------

        ResetRoomUI();
    }

    // =========================================================
    // RESET UI
    // =========================================================

    private void ResetRoomUI()
    {
        updatingRoom = false;

        if (roomPanel != null)
            roomPanel.SetActive(false);

        if (createJoinPanel != null)
            createJoinPanel.SetActive(true);

        if (createRoomButton != null)
            createRoomButton.interactable = true;

        if (joinRoomButton != null)
            joinRoomButton.interactable = true;

        if (joinCodeInput != null)
        {
            joinCodeInput.text = "";
            joinCodeInput.gameObject.SetActive(false);
        }

        if (confirmJoinButton != null)
        {
            confirmJoinButton.interactable = true;
            confirmJoinButton.gameObject.SetActive(false);
        }

        if (roomCodeText != null)
            roomCodeText.text = "";

        if (playersText != null)
            playersText.text = "";

        if (statusText != null)
            statusText.text = "";

        HideJoinError();
    }

    // =========================================================
    // COMENZAR PARTIDA
    // =========================================================

    private void StartGame()
    {
        Debug.Log(
            "[MultiplayerUI] StartGame presionado."
        );

        if (MultiplayerManager.Instance == null)
        {
            Debug.LogError(
                "[MultiplayerUI] No existe MultiplayerManager."
            );

            return;
        }

        updatingRoom = false;

        // El Manager se encarga del NetworkManager
        // y del cambio de escena.
        MultiplayerManager.Instance.StartGame();
    }

    // =========================================================
    // MENÚ JOIN
    // =========================================================

    private void ShowJoinMenu()
    {
        Debug.Log(
            "[MultiplayerUI] ShowJoinMenu presionado."
        );

        if (joinCodeInput != null)
        {
            joinCodeInput.gameObject.SetActive(true);

            joinCodeInput.text = "";

            joinCodeInput.Select();

            joinCodeInput.ActivateInputField();
        }

        if (confirmJoinButton != null)
        {
            confirmJoinButton.gameObject.SetActive(true);

            confirmJoinButton.interactable = true;
        }

        HideJoinError();
    }

    // =========================================================
    // CAMBIO DEL CÓDIGO
    // =========================================================

    private void OnJoinCodeChanged(string value)
    {
        HideJoinError();
    }

    // =========================================================
    // ERROR JOIN
    // =========================================================

    private void ShowJoinError(string message)
    {
        if (invalidCodeText == null)
            return;

        invalidCodeText.text = message;

        invalidCodeText.gameObject.SetActive(true);
    }

    private void HideJoinError()
    {
        if (invalidCodeText != null)
            invalidCodeText.gameObject.SetActive(false);
    }

    // =========================================================
    // UNIRSE A SALA
    // =========================================================

    private async void JoinRoom()
    {
        Debug.Log(
            "[MultiplayerUI] JoinRoom presionado."
        );

        if (MultiplayerManager.Instance == null ||
            joinCodeInput == null ||
            confirmJoinButton == null)
        {
            return;
        }

        string code =
            joinCodeInput.text.Trim().ToUpper();

        // -----------------------------------------------------
        // CÓDIGO VACÍO
        // -----------------------------------------------------

        if (string.IsNullOrEmpty(code))
        {
            ShowJoinError("Código vacío.");
            return;
        }

        HideJoinError();

        confirmJoinButton.interactable = false;

        if (statusText != null)
            statusText.text = "Buscando sala...";

        // -----------------------------------------------------
        // MANAGER SE ENCARGA DEL JOIN
        // -----------------------------------------------------

        bool success =
            await MultiplayerManager.Instance.JoinRoom(code);

        if (this == null || !gameObject)
            return;

        // -----------------------------------------------------
        // JOIN CORRECTO
        // -----------------------------------------------------

        if (success)
        {
            ShowRoomPanel();

            if (roomCodeText != null)
                roomCodeText.text =
                    "Código: " + code;

            if (playersText != null)
                playersText.text =
                    "Jugadores: 2/2";

            if (statusText != null)
                statusText.text =
                    "Esperando al anfitrión...";

            if (startGameButton != null)
                startGameButton.interactable = false;

            StartRoomUpdater();
        }
        else
        {
            // -------------------------------------------------
            // ERROR
            // -------------------------------------------------

            ShowJoinError(
                MultiplayerManager.Instance.LastJoinError
            );

            confirmJoinButton.interactable = true;
        }
    }

    // =========================================================
    // VOLVER AL MENÚ PRINCIPAL
    // =========================================================

    private void ReturnToMainMenu()
    {
        Debug.Log(
            "[MultiplayerUI] ReturnMenuButton presionado."
        );

        updatingRoom = false;

        // -----------------------------------------------------
        // EVITAR DOBLE CLICK
        // -----------------------------------------------------

        if (returnMenuButton != null)
        {
            returnMenuButton.interactable = false;
        }

        // -----------------------------------------------------
        // EL MANAGER SE ENCARGA DE TODO LO MULTIJUGADOR
        // -----------------------------------------------------

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.ReturnToMainMenu();
        }
        else
        {
            // Este caso solamente debería ocurrir si por alguna
            // razón el Manager no existe.
            Debug.LogWarning(
                "[MultiplayerUI] MultiplayerManager no existe. " +
                "Cargando MainMenuScene directamente."
            );

            SceneManager.LoadScene("MainMenuScene");
        }
    }
}