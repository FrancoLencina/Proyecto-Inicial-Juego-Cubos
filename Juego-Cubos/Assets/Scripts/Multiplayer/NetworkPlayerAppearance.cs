using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerAppearance : NetworkBehaviour
{
    [Header("Character Renderer")]
    [SerializeField] private SkinnedMeshRenderer characterRenderer;

    [Header("Player Materials")]
    [SerializeField] private Material playerOneMaterial;
    [SerializeField] private Material playerTwoMaterial;


    public override void OnNetworkSpawn()
    {
        ApplyAppearance();
    }


    private void ApplyAppearance()
    {
        if (characterRenderer == null)
        {
            characterRenderer =
                GetComponentInChildren<SkinnedMeshRenderer>();
        }


        if (characterRenderer == null)
        {
            Debug.LogError(
                "NetworkPlayerAppearance: No se encontró SkinnedMeshRenderer."
            );

            return;
        }


        // =====================================================
        // PLAYER 1 - HOST
        // =====================================================

        if (OwnerClientId ==
            NetworkManager.ServerClientId)
        {
            characterRenderer.material =
                playerOneMaterial;
        }


        // =====================================================
        // PLAYER 2 - CLIENTE
        // =====================================================

        else
        {
            characterRenderer.material =
                playerTwoMaterial;
        }
    }
}