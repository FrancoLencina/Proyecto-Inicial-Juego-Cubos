using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Player Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private NetworkPlayerInteraction networkPlayerInteraction;

    [Header("Player Vision")]
    [SerializeField] private GameObject playerVision;

    private bool setupCompleted;


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // PlayerVision pertenece al Player
        SetupVision();

        // Intentar configurar el jugador
        TrySetupPlayer();
    }


    // =========================================================
    // CONFIGURAR PLAYER VISION
    // =========================================================

    private void SetupVision()
    {
        if (playerVision == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: No se asignó PlayerVision."
            );

            return;
        }

        // Solo el jugador local necesita
        // su PlayerVision activo.
        playerVision.SetActive(IsOwner);
    }


    // =========================================================
    // ESCENA CARGADA
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        TrySetupPlayer();
    }


    // =========================================================
    // CONFIGURAR PLAYER
    // =========================================================

    private void TrySetupPlayer()
    {
        if (setupCompleted)
        {
            return;
        }

        // Solo configuramos el Player local.
        if (!IsOwner)
        {
            return;
        }

        // Solo necesitamos hacer esto en MapScene.
        if (SceneManager.GetActiveScene().name != "MapScene")
        {
            return;
        }


        // =====================================================
        // PLAYER MOVEMENT
        // =====================================================

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        else
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "No se encontró PlayerMovement en el Player."
            );

            return;
        }


        // =====================================================
        // PLAYER VISION
        // =====================================================

        if (playerVision == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "PlayerVision no está asignado."
            );

            return;
        }

        // Solo el Player local utiliza PlayerVision.
        playerVision.SetActive(true);


        // =====================================================
        // CÁMARA DE PLAYER VISION
        // =====================================================

        Camera visionCamera =
            playerVision.GetComponent<Camera>();

        if (visionCamera == null)
        {
            visionCamera =
                playerVision.GetComponentInChildren<Camera>();
        }

        if (visionCamera == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "No se encontró una Camera dentro de PlayerVision."
            );

            return;
        }


        // =====================================================
        // HOLD POINT
        // =====================================================

        Transform holdPoint =
            playerVision.transform.Find("Hold Point");

        if (holdPoint == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "No se encontró Hold Point dentro de PlayerVision."
            );

            return;
        }


        // =====================================================
        // NETWORK PLAYER INTERACTION
        // =====================================================

        if (networkPlayerInteraction == null)
        {
            networkPlayerInteraction =
                GetComponent<NetworkPlayerInteraction>();
        }

        if (networkPlayerInteraction != null)
        {
            networkPlayerInteraction.enabled = true;

            networkPlayerInteraction.SetPlayerCamera(
                visionCamera
            );

            networkPlayerInteraction.SetHoldPoint(
                holdPoint
            );
        }
        else
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "No se encontró NetworkPlayerInteraction en el Player."
            );

            return;
        }


        // =====================================================
        // PLAYER CAMERA
        // =====================================================
        //
        // IMPORTANTE:
        //
        // PlayerCamera NO está dentro del Player.
        //
        // Es un GameObject independiente de MapScene.
        //
        // Buscamos el componente PlayerCamera directamente
        // en la escena.
        // =====================================================

        PlayerCamera cameraController =
            FindAnyObjectByType<PlayerCamera>();

        if (cameraController == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: " +
                "No se encontró un objeto con el componente PlayerCamera " +
                "en MapScene."
            );

            return;
        }


        // =====================================================
        // ASIGNAR PLAYER A LA CÁMARA
        // =====================================================

        cameraController.SetPlayer(
            transform
        );


        // =====================================================
        // ACTIVAR CÁMARA PRINCIPAL
        // =====================================================

        Camera mainCamera =
            cameraController.GetComponent<Camera>();

        if (mainCamera == null)
        {
            mainCamera =
                cameraController.GetComponentInChildren<Camera>();
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
        else
        {
            Debug.LogWarning(
                "PlayerNetworkSetup: " +
                "PlayerCamera no tiene una Camera."
            );
        }


        // =====================================================
        // CONFIGURACIÓN COMPLETADA
        // =====================================================

        setupCompleted = true;

        Debug.Log(
            "PlayerNetworkSetup: " +
            "Player local configurado correctamente."
        );
    }


    // =========================================================
    // NETWORK DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
