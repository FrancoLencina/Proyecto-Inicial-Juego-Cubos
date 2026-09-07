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

    // =========================================================
    // SESIONES
    // =========================================================

    private IHostSession currentSession;
    private ISession joinedSession;

    // =========================================================
    // ESTADO
    // =========================================================

    private bool servicesInitialized = false;
    private bool isLeavingRoom = false;
    private bool isReturningToMenu = false;

    public event Action SessionClosed;

    public string LastJoinError { get; private set; }

    public bool IsHost => currentSession != null;

    // =========================================================
    // UNITY
    // =========================================================

    private async void Awake()
    {
        // -----------------------------------------------------
        // SINGLETON
        // -----------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[MultiplayerManager] Ya existe una instancia. " +
                "Destruyendo la nueva."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Este objeto sí permanece entre escenas.
        DontDestroyOnLoad(gameObject);

        Debug.Log("[MultiplayerManager] Manager iniciado.");

        // -----------------------------------------------------
        // UNITY SERVICES
        // -----------------------------------------------------

        await InitializeUnityServices();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================
    // UNITY SERVICES
    // =========================================================

    private async Task InitializeUnityServices()
    {
        if (servicesInitialized)
            return;

        try
        {
            // Si ya están inicializados, no hacemos nada.
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log(
                    "[MultiplayerManager] Inicializando Unity Services..."
                );

                await UnityServices.InitializeAsync();
            }

            // -------------------------------------------------
            // AUTENTICACIÓN
            // -------------------------------------------------

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log(
                    "[MultiplayerManager] Iniciando sesión anónima..."
                );

                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            servicesInitialized = true;

            Debug.Log(
                "[MultiplayerManager] Unity Services inicializado correctamente."
            );

            Debug.Log(
                "[MultiplayerManager] Player ID: " +
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception e)
        {
            servicesInitialized = false;

            Debug.LogError(
                "[MultiplayerManager] Error inicializando Unity Services:"
            );

            Debug.LogError(e);
        }
    }

    private async Task<bool> WaitForServices()
    {
        if (servicesInitialized)
            return true;

        await InitializeUnityServices();

        return servicesInitialized;
    }

    // =========================================================
    // CREAR SALA
    // =========================================================

    public async Task<string> CreateRoom()
    {
        if (!await WaitForServices())
        {
            Debug.LogError(
                "[MultiplayerManager] Unity Services no está disponible."
            );

            return null;
        }

        try
        {
            if (HasActiveSession())
            {
                Debug.LogWarning(
                    "[MultiplayerManager] Ya existe una sesión activa."
                );

                return null;
            }

            Debug.Log(
                "[MultiplayerManager] Creando sala..."
            );

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = 2
            }.WithRelayNetwork();

            currentSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            Debug.Log(
                "[MultiplayerManager] Sala creada correctamente."
            );

            Debug.Log(
                "[MultiplayerManager] Código de sala: " +
                currentSession.Code
            );

            return currentSession.Code;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[MultiplayerManager] Error creando la sala:"
            );

            Debug.LogError(e);

            currentSession = null;

            return null;
        }
    }

    // =========================================================
    // UNIRSE A SALA
    // =========================================================

    public async Task<bool> JoinRoom(string code)
    {
        LastJoinError = "No se pudo conectar a la sala.";

        if (!await WaitForServices())
        {
            LastJoinError =
                "Los servicios multijugador no están disponibles.";

            return false;
        }

        try
        {
            // -------------------------------------------------
            // VALIDAR CÓDIGO
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(code))
            {
                LastJoinError = "Código vacío.";

                Debug.LogError(
                    "[MultiplayerManager] El código está vacío."
                );

                return false;
            }

            // -------------------------------------------------
            // VALIDAR SESIÓN ACTIVA
            // -------------------------------------------------

            if (HasActiveSession())
            {
                LastJoinError =
                    "Ya existe una sesión activa.";

                Debug.LogWarning(
                    "[MultiplayerManager] Intento de unirse " +
                    "teniendo una sesión activa."
                );

                return false;
            }

            string normalizedCode =
                code.Trim().ToUpper();

            Debug.Log(
                "[MultiplayerManager] Intentando unirse a la sala: " +
                normalizedCode
            );

            JoinSessionOptions options =
                new JoinSessionOptions();

            joinedSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(
                        normalizedCode,
                        options
                    );

            // -------------------------------------------------
            // EVENTO DE SESIÓN ELIMINADA
            // -------------------------------------------------

            joinedSession.Deleted += OnSessionDeleted;

            Debug.Log(
                "[MultiplayerManager] Conectado a la sala: " +
                joinedSession.Code
            );

            LastJoinError = null;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[MultiplayerManager] Error uniéndose a la sala:"
            );

            Debug.LogError(e);

            joinedSession = null;

            // -------------------------------------------------
            // INTERPRETAR ERROR
            // -------------------------------------------------

            string errorMessage =
                e.Message.ToLower();

            if (errorMessage.Contains("not found") ||
                errorMessage.Contains("does not exist") ||
                errorMessage.Contains("invalid"))
            {
                LastJoinError = "Código inválido.";
            }
            else if (
                errorMessage.Contains("network") ||
                errorMessage.Contains("connection") ||
                errorMessage.Contains("timeout") ||
                errorMessage.Contains("service") ||
                errorMessage.Contains("authentication") ||
                errorMessage.Contains("internet"))
            {
                LastJoinError =
                    "Problema de conexión con el servicio.";
            }
            else
            {
                LastJoinError =
                    "No se pudo conectar a la sala.";
            }

            return false;
        }
    }

    // =========================================================
    // SESIÓN ELIMINADA POR EL HOST
    // =========================================================

    private void OnSessionDeleted()
    {
        Debug.Log(
            "[MultiplayerManager] La sesión fue eliminada por el Host."
        );

        if (joinedSession != null)
        {
            joinedSession.Deleted -= OnSessionDeleted;
        }

        joinedSession = null;

        ShutdownNetworkManager();

        SessionClosed?.Invoke();
    }

    // =========================================================
    // INFORMACIÓN DE LA SALA
    // =========================================================

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

    public bool HasActiveSession()
    {
        return currentSession != null ||
               joinedSession != null;
    }

    // =========================================================
    // SALIR DE LA SALA
    // =========================================================

    public async Task LeaveRoom()
    {
        // Evita que dos procesos intenten cerrar la sesión
        // simultáneamente.
        if (isLeavingRoom)
        {
            Debug.LogWarning(
                "[MultiplayerManager] Ya se está saliendo de la sala."
            );

            return;
        }

        isLeavingRoom = true;

        Debug.Log(
            "[MultiplayerManager] Iniciando salida de la sala..."
        );

        try
        {
            // -------------------------------------------------
            // HOST
            // -------------------------------------------------

            if (currentSession != null)
            {
                Debug.Log(
                    "[MultiplayerManager] El Host está cerrando la sala..."
                );

                await currentSession.DeleteAsync();

                Debug.Log(
                    "[MultiplayerManager] Sala eliminada correctamente."
                );
            }

            // -------------------------------------------------
            // CLIENTE
            // -------------------------------------------------

            if (joinedSession != null)
            {
                Debug.Log(
                    "[MultiplayerManager] El cliente está abandonando la sala..."
                );

                joinedSession.Deleted -= OnSessionDeleted;

                await joinedSession.LeaveAsync();

                Debug.Log(
                    "[MultiplayerManager] Cliente abandonó la sala."
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[MultiplayerManager] Error saliendo de la sala:"
            );

            Debug.LogError(e);
        }
        finally
        {
            // -------------------------------------------------
            // LIMPIAR REFERENCIAS
            // -------------------------------------------------

            if (joinedSession != null)
            {
                joinedSession.Deleted -= OnSessionDeleted;
            }

            currentSession = null;
            joinedSession = null;

            // -------------------------------------------------
            // APAGAR NETWORK MANAGER
            // -------------------------------------------------

            ShutdownNetworkManager();

            isLeavingRoom = false;

            Debug.Log(
                "[MultiplayerManager] Sesión limpiada correctamente."
            );
        }
    }

    // =========================================================
    // LIMPIAR SESIÓN
    // =========================================================

    public void ClearSession()
    {
        if (joinedSession != null)
        {
            joinedSession.Deleted -= OnSessionDeleted;
        }

        currentSession = null;
        joinedSession = null;

        Debug.Log(
            "[MultiplayerManager] Referencias de sesión limpiadas."
        );
    }

    // =========================================================
    // NETWORK MANAGER
    // =========================================================

    private void ShutdownNetworkManager()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            Debug.Log(
                "[MultiplayerManager] Apagando NetworkManager..."
            );

            NetworkManager.Singleton.Shutdown();
        }
    }

    // =========================================================
    // VOLVER AL MENÚ PRINCIPAL
    // =========================================================

    public async void ReturnToMainMenu()
    {
        // Evita doble click o múltiples llamadas.
        if (isReturningToMenu)
        {
            Debug.LogWarning(
                "[MultiplayerManager] Ya se está volviendo al menú."
            );

            return;
        }

        isReturningToMenu = true;

        Debug.Log(
            "[MultiplayerManager] Volviendo al menú principal..."
        );

        try
        {
            // -------------------------------------------------
            // CERRAR SESIÓN SI EXISTE
            // -------------------------------------------------

            if (HasActiveSession())
            {
                await LeaveRoom();
            }
            else
            {
                ShutdownNetworkManager();
            }

            // -------------------------------------------------
            // LIMPIAR REFERENCIAS
            // -------------------------------------------------

            ClearSession();

            // -------------------------------------------------
            // CARGAR MENÚ
            // -------------------------------------------------

            Debug.Log(
                "[MultiplayerManager] Cargando MainMenuScene..."
            );

            SceneManager.LoadScene("MainMenuScene");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[MultiplayerManager] Error volviendo al menú:"
            );

            Debug.LogError(e);

            // Como último recurso intentamos limpiar y cargar.
            ClearSession();
            ShutdownNetworkManager();

            SceneManager.LoadScene("MainMenuScene");
        }
        finally
        {
            isReturningToMenu = false;
        }
    }

    // =========================================================
    // COMENZAR PARTIDA
    // =========================================================

    public void StartGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "[MultiplayerManager] No existe NetworkManager."
            );

            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning(
                "[MultiplayerManager] Solo el Host puede comenzar la partida."
            );

            return;
        }

        Debug.Log(
            "[MultiplayerManager] Comenzando partida..."
        );

        NetworkManager.Singleton.SceneManager.LoadScene(
            "MapScene",
            LoadSceneMode.Single
        );
    }
}