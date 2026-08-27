using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    private IHostSession currentSession;

    public bool IsHost => currentSession != null;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Unity Services inicializado correctamente.");
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError("Error inicializando Unity Services: " + e);
        }
    }

    // =========================
    // CREAR SALA
    // =========================

    public async Task<string> CreateRoom()
    {
        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = 2
            }.WithRelayNetwork();

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            string code = currentSession.Code;

            Debug.Log("Sala creada.");
            Debug.Log("Código de sala: " + code);

            return code;
        }
        catch (Exception e)
        {
            Debug.LogError("Error creando la sala: " + e);
            return null;
        }
    }

    // =========================
    // UNIRSE A SALA
    // =========================

    public async Task<bool> JoinRoom(string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Debug.LogError("El código está vacío.");
                return false;
            }

            var options = new JoinSessionOptions();

            var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                code.ToUpper(),
                options
            );

            Debug.Log("Se encontró la sala.");
            Debug.Log("Conectado a la sala: " + session.Code);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error uniéndose a la sala: " + e);
            return false;
        }
    }

    // =========================
    // SALIR DE LA SALA
    // =========================

    public async Task LeaveRoom()
    {
        try
        {
            if (currentSession != null)
            {
                await currentSession.LeaveAsync();
                currentSession = null;
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            Debug.Log("Se abandonó la sala.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error saliendo de la sala: " + e);
        }
    }

    // =========================
    // COMENZAR PARTIDA
    // =========================

    public void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No existe NetworkManager.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Solo el Host puede comenzar la partida.");
            return;
        }

        Debug.Log("Comenzando partida...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Game",
            LoadSceneMode.Single
        );
    }
}