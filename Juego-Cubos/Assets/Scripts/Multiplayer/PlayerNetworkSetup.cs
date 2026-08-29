using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Player Components")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteraction playerInteraction;

    private bool setupCompleted;


    // =========================================================
    // NETWORK SPAWN
    // =========================================================

    public override void OnNetworkSpawn()
    {
        // El Player existe en Netcode,
        // pero la cámara puede todavía no existir.
        SceneManager.sceneLoaded += OnSceneLoaded;

        TrySetupPlayer();
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

        // Todavía no estamos en la escena de juego.
        if (SceneManager.GetActiveScene().name != "MapScene")
        {
            return;
        }

        // =====================================================
        // BUSCAR CÁMARA
        // =====================================================

        Camera playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "PlayerNetworkSetup: MapScene cargada, " +
                "pero todavía no se encontró la Main Camera."
            );

            return;
        }


        // =====================================================
        // PLAYER MOVEMENT
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }


        // =====================================================
        // PLAYER INTERACTION
        // =====================================================

        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;

            playerInteraction.SetPlayerCamera(
                playerCamera
            );
        }


        // =====================================================
        // PLAYER CAMERA
        // =====================================================

        PlayerCamera playerCameraController =
            playerCamera.GetComponent<PlayerCamera>();

        if (playerCameraController == null)
        {
            Debug.LogError(
                "PlayerNetworkSetup: La Main Camera " +
                "no tiene el componente PlayerCamera."
            );

            return;
        }

        playerCameraController.SetPlayer(
            transform
        );


        // =====================================================
        // CONFIGURACIÓN COMPLETADA
        // =====================================================

        setupCompleted = true;

        Debug.Log(
            "PlayerNetworkSetup: Player local configurado correctamente."
        );
    }


    // =========================================================
    // DESPAWN
    // =========================================================

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}