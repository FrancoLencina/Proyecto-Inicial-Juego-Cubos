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

private void Start()
{
    // La sala de espera comienza oculta
    roomPanel.SetActive(false);

    // Los elementos para unirse comienzan ocultos
    joinCodeInput.gameObject.SetActive(false);
    confirmJoinButton.gameObject.SetActive(false);

    // Eventos de los botones
    createRoomButton.onClick.AddListener(CreateRoom);
    joinRoomButton.onClick.AddListener(ShowJoinMenu);
    confirmJoinButton.onClick.AddListener(JoinRoom);

    leaveRoomButton.onClick.AddListener(LeaveRoom);
    startGameButton.onClick.AddListener(StartGame);
}

    private async void CreateRoom()
{
    createRoomButton.interactable = false;
    joinRoomButton.interactable = false;

    statusText.text = "Creando sala...";

    string code = await MultiplayerManager.Instance.CreateRoom();

    if (code != null)
    {
        // Ocultar menú
        createJoinPanel.SetActive(false);

        // Mostrar sala de espera
        roomPanel.SetActive(true);

        roomCodeText.text = "Código:" + code;
        playersText.text = "Jugadores: 1/2";
        statusText.text = "Esperando otro jugador...";

        startGameButton.interactable = true;
    }
    else
    {
        statusText.text = "No se pudo crear la sala.";

        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
    }
}

    private async void LeaveRoom()
{
    statusText.text = "Saliendo...";

    await MultiplayerManager.Instance.LeaveRoom();

    roomPanel.SetActive(false);
    createJoinPanel.SetActive(true);

    createRoomButton.interactable = true;
    joinRoomButton.interactable = true;

    // Volver al estado inicial del menú
    joinCodeInput.gameObject.SetActive(false);
    confirmJoinButton.gameObject.SetActive(false);
}

    private void StartGame()
    {
        MultiplayerManager.Instance.StartGame();
    }

    private void ShowJoinMenu()
{
    joinCodeInput.gameObject.SetActive(true);
    confirmJoinButton.gameObject.SetActive(true);

    joinCodeInput.text = "";
    joinCodeInput.Select();
    joinCodeInput.ActivateInputField();
}

    private async void JoinRoom()
    {
        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Ingresá un código.");
            return;
        }

        confirmJoinButton.interactable = false;

        statusText.text = "Buscando sala...";

        bool success = await MultiplayerManager.Instance.JoinRoom(code);

        if (success)
        {
            createJoinPanel.SetActive(false);
            roomPanel.SetActive(true);

            roomCodeText.text = "Código:" + code;
            playersText.text = "Conectado";
            statusText.text = "Esperando al anfitrión...";

            // El cliente no puede iniciar la partida
            startGameButton.interactable = false;
        }
        else
        {
            statusText.text = "No se encontró la sala.";
            confirmJoinButton.interactable = true;
        }
    }
}