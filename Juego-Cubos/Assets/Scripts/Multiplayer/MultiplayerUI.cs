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

[Header("Join Errors")]
[SerializeField] private TMP_Text invalidCodeText;

[Header("Room")]
[SerializeField] private GameObject roomPanel;
[SerializeField] private TMP_Text roomCodeText;
[SerializeField] private TMP_Text playersText;
[SerializeField] private TMP_Text statusText;
[SerializeField] private Button startGameButton;
[SerializeField] private Button leaveRoomButton;

private bool updatingRoom = false;


// =========================================================
// START
// =========================================================

private void Start()
{
    // Sala oculta al iniciar
    if (roomPanel != null)
    {
        roomPanel.SetActive(false);
    }

    // Elementos de unirse ocultos
    if (joinCodeInput != null)
    {
        joinCodeInput.gameObject.SetActive(false);
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.gameObject.SetActive(false);
    }

    // Mensaje de error oculto
    if (invalidCodeText != null)
    {
        invalidCodeText.gameObject.SetActive(false);
    }

    // Eventos
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

    // Ocultar mensaje de error cuando se escribe
    if (joinCodeInput != null)
    {
        joinCodeInput.onValueChanged.AddListener(OnJoinCodeChanged);
    }

    // Escuchar cuando el Host cierra la sala
    if (MultiplayerManager.Instance != null)
    {
        MultiplayerManager.Instance.SessionClosed += OnSessionClosed;
    }
}


// =========================================================
// ON DESTROY
// =========================================================

private void OnDestroy()
{
    updatingRoom = false;

    if (joinCodeInput != null)
    {
        joinCodeInput.onValueChanged.RemoveListener(
            OnJoinCodeChanged
        );
    }

    if (createRoomButton != null)
    {
        createRoomButton.onClick.RemoveListener(
            CreateRoom
        );
    }

    if (joinRoomButton != null)
    {
        joinRoomButton.onClick.RemoveListener(
            ShowJoinMenu
        );
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.onClick.RemoveListener(
            JoinRoom
        );
    }

    if (leaveRoomButton != null)
    {
        leaveRoomButton.onClick.RemoveListener(
            LeaveRoom
        );
    }

    if (startGameButton != null)
    {
        startGameButton.onClick.RemoveListener(
            StartGame
        );
    }

    if (MultiplayerManager.Instance != null)
    {
        MultiplayerManager.Instance.SessionClosed -=
            OnSessionClosed;
    }
}


// =========================================================
// CREAR SALA
// =========================================================

private async void CreateRoom()
{
    if (createRoomButton != null)
    {
        createRoomButton.interactable = false;
    }

    if (joinRoomButton != null)
    {
        joinRoomButton.interactable = false;
    }

    if (statusText != null)
    {
        statusText.text = "Creando sala...";
    }

    string code =
        await MultiplayerManager.Instance.CreateRoom();

    // La UI puede haber sido destruida mientras esperábamos
    if (this == null ||
        !gameObject)
    {
        return;
    }

    if (code != null)
    {
        if (createJoinPanel != null)
        {
            createJoinPanel.SetActive(false);
        }

        if (roomPanel != null)
        {
            roomPanel.SetActive(true);
        }

        if (roomCodeText != null)
        {
            roomCodeText.text =
                "Código: " + code;
        }

        if (playersText != null)
        {
            playersText.text =
                "Jugadores: 1/2";
        }

        if (statusText != null)
        {
            statusText.text =
                "Esperando otro jugador...";
        }

        // No puede comenzar todavía
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        StartRoomUpdater();
    }
    else
    {
        if (statusText != null)
        {
            statusText.text =
                "No se pudo crear la sala.";
        }

        if (createRoomButton != null)
        {
            createRoomButton.interactable = true;
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = true;
        }
    }
}


// =========================================================
// ACTUALIZAR SALA
// =========================================================

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
    while (updatingRoom)
    {
        // Esperar un segundo entre actualizaciones
        await Task.Delay(1000);

        // =================================================
        // LA UI PUEDE HABER SIDO DESTRUIDA
        // =================================================

        if (this == null ||
            !gameObject)
        {
            return;
        }

        // =================================================
        // VERIFICAR REFERENCIAS DE LA UI
        // =================================================

        if (roomPanel == null ||
            playersText == null ||
            statusText == null ||
            startGameButton == null)
        {
            updatingRoom = false;
            return;
        }

        // =================================================
        // VERIFICAR MULTIPLAYER MANAGER
        // =================================================

        if (MultiplayerManager.Instance == null)
        {
            updatingRoom = false;
            return;
        }

        // =================================================
        // YA NO HAY SESIÓN
        // =================================================

        if (!MultiplayerManager.Instance.HasActiveSession())
        {
            updatingRoom = false;
            return;
        }

        // =================================================
        // ACTUALIZAR CANTIDAD DE JUGADORES
        // =================================================

        int players =
            MultiplayerManager.Instance.GetPlayerCount();

        playersText.text =
            $"Jugadores: {players}/2";


        // =================================================
        // HOST
        // =================================================

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


        // =================================================
        // CLIENTE
        // =================================================

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
// HOST CERRÓ LA SALA
// =========================================================

private void OnSessionClosed()
{
    Debug.Log(
        "El Host cerró la sala."
    );

    // Detener actualización
    updatingRoom = false;

    // Verificar que la UI todavía exista
    if (this == null ||
        !gameObject)
    {
        return;
    }

    if (statusText != null)
    {
        statusText.text =
            "El anfitrión cerró la sala.";
    }

    if (playersText != null)
    {
        playersText.text = "";
    }

    // Desconectar Netcode
    if (
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.IsListening
    )
    {
        Unity.Netcode.NetworkManager.Singleton.Shutdown();
    }

    // Volver al menú después de un pequeño momento
    Invoke(
        nameof(ReturnToMenuAfterHostClosed),
        1.5f
    );
}


private void ReturnToMenuAfterHostClosed()
{
    if (this == null ||
        !gameObject)
    {
        return;
    }

    if (roomPanel != null)
    {
        roomPanel.SetActive(false);
    }

    if (createJoinPanel != null)
    {
        createJoinPanel.SetActive(true);
    }

    if (createRoomButton != null)
    {
        createRoomButton.interactable = true;
    }

    if (joinRoomButton != null)
    {
        joinRoomButton.interactable = true;
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.interactable = true;
    }

    if (joinCodeInput != null)
    {
        joinCodeInput.gameObject.SetActive(false);
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.gameObject.SetActive(false);
    }

    HideJoinError();

    if (roomCodeText != null)
    {
        roomCodeText.text = "";
    }

    if (playersText != null)
    {
        playersText.text = "";
    }

    if (statusText != null)
    {
        statusText.text = "";
    }

    updatingRoom = false;
}


// =========================================================
// SALIR DE LA SALA
// =========================================================

private async void LeaveRoom()
{
    if (statusText != null)
    {
        statusText.text =
            "Saliendo...";
    }

    await MultiplayerManager.Instance.LeaveRoom();

    if (this == null ||
        !gameObject)
    {
        return;
    }

    ResetRoomUI();
}


// =========================================================
// RESET UI
// =========================================================

private void ResetRoomUI()
{
    updatingRoom = false;

    if (roomPanel != null)
    {
        roomPanel.SetActive(false);
    }

    if (createJoinPanel != null)
    {
        createJoinPanel.SetActive(true);
    }

    if (createRoomButton != null)
    {
        createRoomButton.interactable = true;
    }

    if (joinRoomButton != null)
    {
        joinRoomButton.interactable = true;
    }

    if (joinCodeInput != null)
    {
        joinCodeInput.gameObject.SetActive(false);
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.gameObject.SetActive(false);
        confirmJoinButton.interactable = true;
    }

    HideJoinError();

    if (roomCodeText != null)
    {
        roomCodeText.text = "";
    }

    if (playersText != null)
    {
        playersText.text = "";
    }

    if (statusText != null)
    {
        statusText.text = "";
    }
}


// =========================================================
// COMENZAR PARTIDA
// =========================================================

private void StartGame()
{
    if (MultiplayerManager.Instance == null)
    {
        return;
    }

    MultiplayerManager.Instance.StartGame();
}


// =========================================================
// MOSTRAR MENU UNIRSE
// =========================================================

private void ShowJoinMenu()
{
    if (joinCodeInput != null)
    {
        joinCodeInput.gameObject.SetActive(true);
    }

    if (confirmJoinButton != null)
    {
        confirmJoinButton.gameObject.SetActive(true);
        confirmJoinButton.interactable = true;
    }

    HideJoinError();

    if (joinCodeInput != null)
    {
        joinCodeInput.text = "";
        joinCodeInput.Select();
        joinCodeInput.ActivateInputField();
    }
}


// =========================================================
// CAMBIO EN EL INPUT
// =========================================================

private void OnJoinCodeChanged(string value)
{
    HideJoinError();
}


// =========================================================
// MOSTRAR ERROR
// =========================================================

private void ShowJoinError(string message)
{
    if (invalidCodeText == null)
    {
        return;
    }

    invalidCodeText.text = message;
    invalidCodeText.gameObject.SetActive(true);
}


// =========================================================
// OCULTAR ERROR
// =========================================================

private void HideJoinError()
{
    if (invalidCodeText != null)
    {
        invalidCodeText.gameObject.SetActive(false);
    }
}


// =========================================================
// UNIRSE A SALA
// =========================================================

private async void JoinRoom()
{
    if (joinCodeInput == null ||
        confirmJoinButton == null)
    {
        return;
    }

    string code =
        joinCodeInput.text.Trim().ToUpper();


    // =====================================================
    // CÓDIGO VACÍO
    // =====================================================

    if (string.IsNullOrEmpty(code))
    {
        ShowJoinError(
            "Código vacío."
        );

        return;
    }

    HideJoinError();

    confirmJoinButton.interactable = false;

    if (statusText != null)
    {
        statusText.text =
            "Buscando sala...";
    }

    bool success =
        await MultiplayerManager.Instance.JoinRoom(code);

    // La UI puede haber sido destruida durante el await
    if (this == null ||
        !gameObject)
    {
        return;
    }


    // =====================================================
    // CONEXIÓN EXITOSA
    // =====================================================

    if (success)
    {
        if (createJoinPanel != null)
        {
            createJoinPanel.SetActive(false);
        }

        if (roomPanel != null)
        {
            roomPanel.SetActive(true);
        }

        if (roomCodeText != null)
        {
            roomCodeText.text =
                "Código: " + code;
        }

        if (playersText != null)
        {
            playersText.text =
                "Jugadores: 2/2";
        }

        if (statusText != null)
        {
            statusText.text =
                "Esperando al anfitrión...";
        }

        // El cliente nunca puede iniciar
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        StartRoomUpdater();
    }


    // =====================================================
    // ERROR
    // =====================================================

    else
    {
        ShowJoinError(
            MultiplayerManager.Instance.LastJoinError
        );

        // Puede volver a intentar
        if (confirmJoinButton != null)
        {
            confirmJoinButton.interactable = true;
        }
    }
}
}
