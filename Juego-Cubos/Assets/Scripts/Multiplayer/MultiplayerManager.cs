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
    private ISession joinedSession;

    // Evento que avisa a la UI cuando el Host cerró la sala
    public event Action SessionClosed;

    // Último error ocurrido al intentar unirse
    public string LastJoinError { get; private set; }

    public bool IsHost => currentSession != null;

    private async void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
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
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            Debug.Log(
                "Unity Services inicializado correctamente."
            );

            Debug.Log(
                "Player ID: " +
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Error inicializando Unity Services: " +
                e
            );
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

            currentSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            string code =
                currentSession.Code;

            Debug.Log("Sala creada.");
            Debug.Log(
                "Código de sala: " +
                code
            );

            return code;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Error creando la sala: " +
                e
            );

            return null;
        }
    }

    // =========================
    // UNIRSE A SALA
    // =========================

    public async Task<bool> JoinRoom(string code)
    {
        // Valor por defecto
        LastJoinError =
            "No se pudo conectar a la sala.";

        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                LastJoinError =
                    "Código vacío.";

                Debug.LogError(
                    "El código está vacío."
                );

                return false;
            }

            var options =
                new JoinSessionOptions();

            joinedSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(
                        code.ToUpper(),
                        options
                    );

            // Escuchar cuando el Host elimina/cierra la sesión
            joinedSession.Deleted +=
                OnSessionDeleted;

            Debug.Log(
                "Se encontró la sala."
            );

            Debug.Log(
                "Conectado a la sala: " +
                joinedSession.Code
            );

            // No hay error
            LastJoinError = null;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Error uniéndose a la sala:"
            );

            // IMPORTANTE:
            // Esto permite ver el error técnico
            // completo en la Console de Unity.
            Debug.LogError(
                e.ToString()
            );

            joinedSession = null;

            // Obtener mensaje para el jugador
            string errorMessage =
                e.Message.ToLower();

            // =========================
            // CÓDIGO INVÁLIDO
            // =========================

            if (
                errorMessage.Contains("not found") ||
                errorMessage.Contains("does not exist") ||
                errorMessage.Contains("invalid")
            )
            {
                LastJoinError =
                    "Código inválido.";
            }

            // =========================
            // PROBLEMA DE CONEXIÓN
            // =========================

            else if (
                errorMessage.Contains("network") ||
                errorMessage.Contains("connection") ||
                errorMessage.Contains("timeout") ||
                errorMessage.Contains("service") ||
                errorMessage.Contains("authentication") ||
                errorMessage.Contains("internet")
            )
            {
                LastJoinError =
                    "Problema de conexión con el servicio.";
            }

            // =========================
            // OTRO ERROR
            // =========================

            else
            {
                LastJoinError =
                    "No se pudo conectar a la sala.";
            }

            return false;
        }
    }

    // =========================
    // HOST CERRÓ LA SALA
    // =========================

    private void OnSessionDeleted()
    {
        Debug.Log(
            "La sesión fue eliminada por el Host."
        );

        joinedSession = null;

        // Avisar a MultiplayerUI
        SessionClosed?.Invoke();
    }

    // =========================
    // CANTIDAD DE JUGADORES
    // =========================

    public int GetPlayerCount()
    {
        if (currentSession != null)
        {
            return currentSession.Players.Count;
        }

        if (joinedSession != null)
        {
            return joinedSession.Players.Count;
        }

        return 0;
    }

    // =========================
    // SESIÓN ACTIVA
    // =========================

    public bool HasActiveSession()
    {
        return currentSession != null ||
               joinedSession != null;
    }

    // =========================
    // SALIR DE LA SALA
    // =========================

    public async Task LeaveRoom()
    {
        try
        {
            // =========================
            // HOST
            // =========================

            if (currentSession != null)
            {
                Debug.Log(
                    "El Host está cerrando la sala..."
                );

                // El Host elimina completamente la sesión.
                await currentSession.DeleteAsync();

                currentSession = null;

                Debug.Log(
                    "Sala eliminada correctamente."
                );
            }

            // =========================
            // CLIENTE
            // =========================

            if (joinedSession != null)
            {
                // Dejar de escuchar el evento
                joinedSession.Deleted -=
                    OnSessionDeleted;

                await joinedSession.LeaveAsync();

                joinedSession = null;

                Debug.Log(
                    "Cliente abandonó la sala."
                );
            }

            // =========================
            // NETCODE
            // =========================

            if (
                NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening
            )
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Error saliendo de la sala: " +
                e
            );

            // Limpiar referencias aunque haya ocurrido un error
            currentSession = null;
            joinedSession = null;
        }
    }

    // =========================
    // LIMPIAR SESIÓN
    // =========================

    public void ClearSession()
    {
        if (joinedSession != null)
        {
            joinedSession.Deleted -=
                OnSessionDeleted;
        }

        currentSession = null;
        joinedSession = null;
    }

    // =========================
    // COMENZAR PARTIDA
    // =========================

    public void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "No existe NetworkManager."
            );

            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning(
                "Solo el Host puede comenzar la partida."
            );

            return;
        }

        Debug.Log(
            "Comenzando partida..."
        );

        NetworkManager.Singleton.SceneManager.LoadScene(
            "MapScene",
            LoadSceneMode.Single
        );
    }
}
