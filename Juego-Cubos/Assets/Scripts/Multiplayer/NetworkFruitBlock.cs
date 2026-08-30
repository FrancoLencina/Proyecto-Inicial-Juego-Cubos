using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkFruitBlock : NetworkBehaviour
{
    [Header("Fruit Data")]
    [SerializeField] private List<FruitData> availableFruits;

    private Renderer blockRenderer;

    private NetworkVariable<FruitType> fruitType =
        new NetworkVariable<FruitType>(
            FruitType.Apple,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

private NetworkVariable<bool> isBeingHeld =
    new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

private NetworkVariable<ulong> holderClientId =
    new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

public bool IsBeingHeld => isBeingHeld.Value;

public ulong HolderClientId => holderClientId.Value;
    public FruitData FruitData
    {
        get
        {
            if (availableFruits == null)
                return null;

            foreach (FruitData fruit in availableFruits)
            {
                if (fruit != null &&
                    fruit.FruitType == fruitType.Value)
                {
                    return fruit;
                }
            }

            return null;
        }
    }

    public FruitType FruitType => fruitType.Value;

    private void Awake()
    {
        blockRenderer = GetComponent<Renderer>();

        if (blockRenderer == null)
        {
            blockRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        fruitType.OnValueChanged += OnFruitTypeChanged;

        Debug.Log(
            "[NetworkFruitBlock] Spawned | " +
            "IsServer: " + IsServer +
            " | FruitType: " + fruitType.Value +
            " | AvailableFruits: " +
            (availableFruits == null
                ? "NULL"
                : availableFruits.Count.ToString())
        );

        // Intentamos aplicar el material tanto en Host como Cliente.
        ApplyFruitData();
    }

    public override void OnNetworkDespawn()
    {
        fruitType.OnValueChanged -= OnFruitTypeChanged;
    }

    public void SetFruitData(FruitData fruit)
    {
        if (!IsServer)
            return;

        if (fruit == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] Se intentó asignar una fruta null."
            );

            return;
        }

        Debug.Log(
            "[NetworkFruitBlock] SetFruitData → "
            + fruit.DisplayName
            + " / "
            + fruit.FruitType
        );

        fruitType.Value = fruit.FruitType;

        ApplyFruitData();
    }

    private void OnFruitTypeChanged(
        FruitType previousValue,
        FruitType newValue)
    {
        Debug.Log(
            "[NetworkFruitBlock] FruitTypeChanged → "
            + previousValue
            + " → "
            + newValue
        );

        ApplyFruitData();
    }

    private void ApplyFruitData()
    {
        if (availableFruits == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] Available Fruits es NULL."
            );

            return;
        }

        if (availableFruits.Count == 0)
        {
            Debug.LogError(
                "[NetworkFruitBlock] Available Fruits está vacío."
            );

            return;
        }

        FruitData fruit = null;

        foreach (FruitData data in availableFruits)
        {
            if (data == null)
                continue;

            if (data.FruitType == fruitType.Value)
            {
                fruit = data;
                break;
            }
        }

        if (fruit == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] No existe FruitData para FruitType: "
                + fruitType.Value
            );

            return;
        }

        if (blockRenderer == null)
        {
            blockRenderer = GetComponent<Renderer>();

            if (blockRenderer == null)
            {
                blockRenderer =
                    GetComponentInChildren<Renderer>();
            }
        }

        if (blockRenderer == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] No se encontró Renderer."
            );

            return;
        }

        if (fruit.Material == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] "
                + fruit.DisplayName
                + " no tiene Material."
            );

            return;
        }

        blockRenderer.material = fruit.Material;

        Debug.Log(
            "[NetworkFruitBlock] Material aplicado → "
            + fruit.DisplayName
            + " / "
            + fruit.FruitType
        );
    }

    public void SetHeldState(
    bool held,
    ulong clientId)
{
    if (!IsServer)
        return;

    isBeingHeld.Value = held;
    holderClientId.Value = clientId;
}
}