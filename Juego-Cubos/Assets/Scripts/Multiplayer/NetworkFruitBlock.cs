using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkFruitBlock : NetworkBehaviour
{
    [Header("Fruit Data")]
    [SerializeField] private List<FruitData> availableFruits;

    private Renderer blockRenderer;
    private Rigidbody blockRigidbody;

    // =========================================================
    // NETWORK VARIABLES
    // =========================================================

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


    // =========================================================
    // FRUIT DATA
    // =========================================================

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


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        blockRenderer = GetComponent<Renderer>();

        if (blockRenderer == null)
        {
            blockRenderer =
                GetComponentInChildren<Renderer>();
        }

        blockRigidbody =
            GetComponent<Rigidbody>();
    }


    public override void OnNetworkSpawn()
    {
        fruitType.OnValueChanged += OnFruitTypeChanged;

        isBeingHeld.OnValueChanged += OnHeldStateChanged;

        Debug.Log(
            "[NetworkFruitBlock] Spawned | " +
            "IsServer: " + IsServer +
            " | IsOwner: " + IsOwner +
            " | FruitType: " + fruitType.Value
        );

        ApplyFruitData();

        ApplyPhysicsState();
    }


    public override void OnNetworkDespawn()
    {
        fruitType.OnValueChanged -= OnFruitTypeChanged;

        isBeingHeld.OnValueChanged -= OnHeldStateChanged;
    }


    // =========================================================
    // PHYSICS
    // =========================================================

    private void OnHeldStateChanged(
        bool previousValue,
        bool newValue)
    {
        ApplyPhysicsState();
    }


    private void ApplyPhysicsState()
    {
        if (blockRigidbody == null)
        {
            blockRigidbody =
                GetComponent<Rigidbody>();
        }

        if (blockRigidbody == null)
            return;


        // -----------------------------------------------------
        // BLOQUE AGARRADO
        // -----------------------------------------------------

        if (isBeingHeld.Value)
        {
            // Primero aseguramos que el Rigidbody todavía
            // permita modificar velocidades.

            if (!blockRigidbody.isKinematic)
            {
                blockRigidbody.linearVelocity =
                    Vector3.zero;

                blockRigidbody.angularVelocity =
                    Vector3.zero;
            }


            blockRigidbody.isKinematic =
                true;

            blockRigidbody.useGravity =
                false;

            return;
        }


        // -----------------------------------------------------
        // BLOQUE LIBRE
        // -----------------------------------------------------

        // La física de los bloques libres pertenece
        // al servidor.
        //
        // En el Host:
        // IsServer = true -> física dinámica.
        //
        // En los clientes:
        // IsServer = false -> representación kinematic.
        //
        // De esta manera no existen dos simulaciones
        // físicas diferentes del mismo bloque.

        if (IsServer)
        {
            blockRigidbody.isKinematic =
                false;

            blockRigidbody.useGravity =
                true;
        }
        else
        {
            blockRigidbody.isKinematic =
                true;

            blockRigidbody.useGravity =
                false;
        }
    }


    // =========================================================
    // FRUIT
    // =========================================================

    public void SetFruitData(FruitData fruit)
    {
        if (!IsServer)
            return;

        if (fruit == null)
        {
            Debug.LogError(
                "[NetworkFruitBlock] " +
                "Se intentó asignar una fruta null."
            );

            return;
        }

        fruitType.Value =
            fruit.FruitType;

        ApplyFruitData();
    }


    private void OnFruitTypeChanged(
        FruitType previousValue,
        FruitType newValue)
    {
        ApplyFruitData();
    }


    private void ApplyFruitData()
    {
        if (availableFruits == null)
            return;


        FruitData fruit = null;


        foreach (FruitData data in availableFruits)
        {
            if (data != null &&
                data.FruitType == fruitType.Value)
            {
                fruit = data;
                break;
            }
        }


        if (fruit == null)
            return;


        if (blockRenderer == null)
        {
            blockRenderer =
                GetComponentInChildren<Renderer>();
        }


        if (blockRenderer == null)
            return;


        if (fruit.Material == null)
            return;


        blockRenderer.material =
            fruit.Material;
    }


    // =========================================================
    // HELD STATE
    // =========================================================

    public void SetHeldState(
        bool held,
        ulong clientId)
    {
        if (!IsServer)
            return;


        isBeingHeld.Value =
            held;

        holderClientId.Value =
            clientId;


        // Aplicar inmediatamente en el servidor.
        ApplyPhysicsState();
    }


    // =========================================================
    // SERVER PHYSICS
    // =========================================================

    /// <summary>
    /// Aplica una fuerza al bloque desde el servidor.
    /// Se utiliza para que los clientes puedan empujar
    /// bloques sin simular físicamente el bloque localmente.
    /// </summary>
    public void ApplyServerImpulse(
        Vector3 impulse)
    {
        if (!IsServer)
            return;

        if (isBeingHeld.Value)
            return;

        if (blockRigidbody == null)
        {
            blockRigidbody =
                GetComponent<Rigidbody>();
        }

        if (blockRigidbody == null)
            return;

        if (blockRigidbody.isKinematic)
            return;


        blockRigidbody.AddForce(
            impulse,
            ForceMode.Impulse
        );
    }

    // =========================================================
    // NETWORK PUSH
    // =========================================================

    /// <summary>
    /// Solicita un empuje sobre este bloque.
    ///
    /// Si la llamada ocurre en el servidor, la fuerza se aplica
    /// inmediatamente.
    ///
    /// Si ocurre en un cliente, se envía una solicitud al servidor.
    /// </summary>
    public void RequestPush(
        Vector3 force
    )
    {
        if (force.sqrMagnitude <
            0.000001f)
        {
            return;
        }


        if (IsBeingHeld)
        {
            return;
        }


        if (IsServer)
        {
            ApplyNetworkPush(
                force
            );
        }
        else
        {
            RequestPushServerRpc(
                force
            );
        }
    }


    // =========================================================
    // SERVER RPC - REQUEST PUSH
    // =========================================================

    [ServerRpc(
        RequireOwnership = false
    )]
    private void RequestPushServerRpc(
        Vector3 force
    )
    {
        ApplyNetworkPush(
            force
        );
    }


    // =========================================================
    // APPLY NETWORK PUSH
    // =========================================================

    private void ApplyNetworkPush(
        Vector3 force
    )
    {
        if (!IsServer)
            return;


        if (IsBeingHeld)
            return;


        if (blockRigidbody == null)
        {
            blockRigidbody =
                GetComponent<Rigidbody>();
        }


        if (blockRigidbody == null)
            return;


        if (blockRigidbody.isKinematic)
            return;


        blockRigidbody.AddForce(
            force,
            ForceMode.Force
        );
    }
}
