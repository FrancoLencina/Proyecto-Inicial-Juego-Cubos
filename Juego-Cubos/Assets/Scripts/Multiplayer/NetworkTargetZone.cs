using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class NetworkTargetZone : MonoBehaviour
{
    [Header("Zone Configuration")]

    [Tooltip(
        "Activado = zona del Host. " +
        "Desactivado = zona del Cliente."
    )]
    [SerializeField] private bool isHostZone;

    [Header("References")]

    [SerializeField] private NetworkGameManager gameManager;

    [SerializeField] private SequenceUI sequenceUI;

    [Header("Block Validation")]

    [SerializeField] private LayerMask validBlockLayer;

    [SerializeField] private float verticalTolerance = 0.05f;

    private List<NetworkFruitBlock> fruitBlocksInside =
        new List<NetworkFruitBlock>();

    private Dictionary<NetworkFruitBlock, ulong> blockOwners =
        new Dictionary<NetworkFruitBlock, ulong>();

    private int currentProgress = 0;

    private int lastSoundProgress = 0;

    private bool sequenceCompleted;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Awake()
    {
        Debug.Log(
            "[NetworkTargetZone] Awake | " +
            "Objeto: " +
            gameObject.name +
            " | Zona: " +
            (isHostZone
                ? "HOST"
                : "CLIENTE")
        );
    }


    private void Start()
    {
        if (gameManager == null)
        {
            gameManager =
                FindAnyObjectByType<NetworkGameManager>();
        }

        if (sequenceUI == null)
        {
            sequenceUI =
                FindAnyObjectByType<SequenceUI>();
        }

        if (gameManager == null)
        {
            Debug.LogError(
                "[NetworkTargetZone] " +
                "NetworkGameManager NO encontrado | " +
                "Zona: " +
                (isHostZone
                    ? "HOST"
                    : "CLIENTE")
            );
        }

        if (sequenceUI == null)
        {
            Debug.LogWarning(
                "[NetworkTargetZone] " +
                "SequenceUI no encontrado todavía | " +
                "Zona: " +
                (isHostZone
                    ? "HOST"
                    : "CLIENTE")
            );
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (
            NetworkManager.Singleton == null ||
            gameManager == null
        )
        {
            return;
        }

        if (
            gameManager.TargetSequence == null ||
            gameManager.TargetSequence.Count == 0
        )
        {
            return;
        }

        /*
         * =================================================
         * SERVIDOR
         * =================================================
         */

        if (
            NetworkManager.Singleton.IsServer
        )
        {
            UpdateServerValidation();
        }

        /*
         * =================================================
         * HUD LOCAL
         * =================================================
         */

        if (
            NetworkManager.Singleton.IsClient &&
            IsLocalPlayerZone()
        )
        {
            UpdateLocalProgressUI();
        }
    }


    // =====================================================
    // VALIDACIÓN DEL SERVIDOR
    // =====================================================

    private void UpdateServerValidation()
    {
        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        int newProgress =
            GetCorrectProgress();

        if (newProgress != currentProgress)
        {
            currentProgress =
                newProgress;

            Debug.Log(
                "[NetworkTargetZone] " +
                "Progreso cambiado | " +
                "Zona: " +
                (isHostZone
                    ? "HOST"
                    : "CLIENTE") +
                " | Progreso: " +
                currentProgress +
                "/" +
                gameManager.TargetSequence.Count
            );
        }

        /*
         * Si ya se detectó la secuencia completa,
         * no volvemos a solicitar otra comprobación.
         *
         * NetworkGameManager será quien espere y confirme.
         */

        if (sequenceCompleted)
            return;

        if (
            currentProgress >=
            gameManager.TargetSequence.Count
        )
        {
            sequenceCompleted = true;

            ulong playerId =
                GetZonePlayerId();

            if (
                playerId !=
                ulong.MaxValue
            )
            {
                Debug.Log(
                    "[NetworkTargetZone] " +
                    "SECUENCIA COMPLETADA | " +
                    "Zona: " +
                    (isHostZone
                        ? "HOST"
                        : "CLIENTE") +
                    " | PlayerId: " +
                    playerId
                );

                gameManager.PlayerCompleted(
                    playerId
                );
            }
        }
    }


    // =====================================================
    // COMPROBAR SI LA PILA SIGUE COMPLETA
    // =====================================================

    public bool IsStackComplete()
    {
        if (gameManager == null)
            return false;

        if (
            gameManager.TargetSequence == null ||
            gameManager.TargetSequence.Count == 0
        )
        {
            return false;
        }

        /*
         * Volvemos a reconstruir la información
         * antes de responder.
         */

        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        int progress =
            GetCorrectProgress();

        currentProgress =
            progress;

        bool complete =
            progress >=
            gameManager.TargetSequence.Count;

        Debug.Log(
            "[NetworkTargetZone] " +
            "COMPROBACIÓN FINAL | " +
            "Zona: " +
            (isHostZone
                ? "HOST"
                : "CLIENTE") +
            " | " +
            progress +
            "/" +
            gameManager.TargetSequence.Count +
            " | Completa: " +
            complete
        );

        return complete;
    }


    // =====================================================
    // RESETEAR ESTADO DE COMPLETADO
    // =====================================================

    public void ResetCompletionState()
    {
        sequenceCompleted = false;

        /*
         * Actualizamos inmediatamente el progreso
         * para que el sistema pueda detectar una nueva
         * finalización posteriormente.
         */

        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        currentProgress =
            GetCorrectProgress();

        Debug.Log(
            "[NetworkTargetZone] " +
            "Estado de completado reiniciado | " +
            "Zona: " +
            (isHostZone
                ? "HOST"
                : "CLIENTE") +
            " | Progreso actual: " +
            currentProgress
        );
    }


    // =====================================================
    // PROGRESO LOCAL DEL HUD
    // =====================================================

    private void UpdateLocalProgressUI()
    {
        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        int localProgress =
            GetCorrectProgress();

        /*
         * =================================================
         * SONIDO
         * =================================================
         */

        if (
            localProgress >
            lastSoundProgress
        )
        {
            if (
                SoundManager.Instance != null
            )
            {
                SoundManager.Instance
                    .PlayCorrectPlacement();
            }
        }

        lastSoundProgress =
            localProgress;

        /*
         * =================================================
         * ACTUALIZAR PROGRESO
         * =================================================
         */

        if (
            localProgress !=
            currentProgress
        )
        {
            currentProgress =
                localProgress;

            Debug.Log(
                "[NetworkTargetZone] " +
                "Progreso HUD local | " +
                "Zona: " +
                (isHostZone
                    ? "HOST"
                    : "CLIENTE") +
                " | Progreso: " +
                currentProgress +
                "/" +
                gameManager.TargetSequence.Count
            );
        }

        UpdateLocalUI(
            localProgress
        );
    }


    // =====================================================
    // DETERMINAR ZONA LOCAL
    // =====================================================

    private bool IsLocalPlayerZone()
    {
        if (
            NetworkManager.Singleton == null
        )
        {
            return false;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        ulong hostClientId =
            NetworkManager.ServerClientId;

        if (isHostZone)
        {
            return localClientId ==
                   hostClientId;
        }

        return localClientId !=
               hostClientId;
    }


    // =====================================================
    // DETECTAR BLOQUES
    // =====================================================

    private void DetectBlocksInsideZone()
    {
        Collider zoneCollider =
            GetComponent<Collider>();

        if (zoneCollider == null)
        {
            return;
        }

        Bounds bounds =
            zoneCollider.bounds;

        Collider[] colliders =
            Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Collide
            );

        foreach (
            Collider collider in colliders
        )
        {
            if (collider == null)
                continue;

            NetworkFruitBlock networkBlock =
                collider.GetComponentInParent<
                    NetworkFruitBlock>();

            if (networkBlock == null)
                continue;

            /*
             * Si ya está registrado,
             * solamente nos aseguramos de que
             * continúe dentro de la lista.
             */

            if (
                blockOwners.ContainsKey(
                    networkBlock
                )
            )
            {
                if (
                    !fruitBlocksInside.Contains(
                        networkBlock
                    )
                )
                {
                    fruitBlocksInside.Add(
                        networkBlock
                    );
                }

                continue;
            }

            ulong playerId =
                networkBlock.HolderClientId;

            /*
             * Se mantiene la lógica que tenías:
             * no filtramos por propietario.
             *
             * Esto evita cambiar tu comportamiento
             * actual de detección.
             */

            if (
                !fruitBlocksInside.Contains(
                    networkBlock
                )
            )
            {
                fruitBlocksInside.Add(
                    networkBlock
                );
            }

            blockOwners[networkBlock] =
                playerId;

            Debug.Log(
                "[NetworkTargetZone] " +
                "BLOQUE REGISTRADO | " +
                "Zona: " +
                (isHostZone
                    ? "HOST"
                    : "CLIENTE") +
                " | PlayerId: " +
                playerId +
                " | Fruta: " +
                networkBlock.FruitType
            );
        }
    }


    // =====================================================
    // TRIGGER ENTER
    // =====================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        NetworkFruitBlock networkBlock =
            other.GetComponentInParent<
                NetworkFruitBlock>();

        if (networkBlock == null)
            return;

        if (
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer
        )
        {
            return;
        }

        if (
            blockOwners.ContainsKey(
                networkBlock
            )
        )
        {
            return;
        }

        ulong playerId =
            networkBlock.HolderClientId;

        if (
            !fruitBlocksInside.Contains(
                networkBlock
            )
        )
        {
            fruitBlocksInside.Add(
                networkBlock
            );
        }

        blockOwners[networkBlock] =
            playerId;

        Debug.Log(
            "[NetworkTargetZone] " +
            "BLOQUE ENTRÓ A LA ZONA | " +
            "Zona: " +
            (isHostZone
                ? "HOST"
                : "CLIENTE") +
            " | PlayerId: " +
            playerId +
            " | Fruta: " +
            networkBlock.FruitType
        );
    }


    // =====================================================
    // TRIGGER EXIT
    // =====================================================

    private void OnTriggerExit(
        Collider other
    )
    {
        if (
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer
        )
        {
            return;
        }

        NetworkFruitBlock networkBlock =
            other.GetComponentInParent<
                NetworkFruitBlock>();

        if (networkBlock == null)
            return;

        if (
            fruitBlocksInside.Contains(
                networkBlock
            )
        )
        {
            fruitBlocksInside.Remove(
                networkBlock
            );
        }

        blockOwners.Remove(
            networkBlock
        );

        Debug.Log(
            "[NetworkTargetZone] " +
            "BLOQUE SALIÓ DE LA ZONA | " +
            "Zona: " +
            (isHostZone
                ? "HOST"
                : "CLIENTE") +
            " | Fruta: " +
            networkBlock.FruitType
        );
    }


    // =====================================================
    // IDENTIFICAR JUGADOR DE LA ZONA
    // =====================================================

    private ulong GetZonePlayerId()
    {
        if (
            NetworkManager.Singleton == null
        )
        {
            return ulong.MaxValue;
        }

        if (isHostZone)
        {
            return NetworkManager.ServerClientId;
        }

        foreach (
            ulong clientId
            in NetworkManager.Singleton
                .ConnectedClientsIds
        )
        {
            if (
                clientId !=
                NetworkManager.ServerClientId
            )
            {
                return clientId;
            }
        }

        return ulong.MaxValue;
    }


    // =====================================================
    // VALIDAR DUEÑO
    // =====================================================

    private bool IsBlockFromCorrectPlayer(
        ulong playerId
    )
    {
        if (
            NetworkManager.Singleton == null
        )
        {
            return false;
        }

        ulong hostClientId =
            NetworkManager.ServerClientId;

        if (isHostZone)
        {
            return playerId ==
                   hostClientId;
        }

        return playerId !=
               hostClientId;
    }


    // =====================================================
    // LIMPIAR BLOQUES
    // =====================================================

    private void CleanInvalidBlocks()
    {
        for (
            int i =
                fruitBlocksInside.Count - 1;
            i >= 0;
            i--
        )
        {
            NetworkFruitBlock networkBlock =
                fruitBlocksInside[i];

            if (networkBlock == null)
            {
                fruitBlocksInside.RemoveAt(i);

                continue;
            }

            Collider blockCollider =
                networkBlock.GetComponent<Collider>();

            if (
                blockCollider == null ||
                !blockCollider.enabled
            )
            {
                fruitBlocksInside.RemoveAt(i);

                blockOwners.Remove(
                    networkBlock
                );
            }
        }
    }


    // =====================================================
    // CALCULAR PROGRESO
    // =====================================================

    private int GetCorrectProgress()
    {
        List<NetworkFruitBlock> orderedBlocks =
            GetBlocksOrderedByHeight();

        if (
            gameManager == null ||
            gameManager.TargetSequence == null
        )
        {
            return 0;
        }

        int correctCount = 0;

        for (
            int i = 0;
            i < orderedBlocks.Count &&
            i < gameManager.TargetSequence.Count;
            i++
        )
        {
            NetworkFruitBlock block =
                orderedBlocks[i];

            if (block == null)
                break;

            if (
                !blockOwners.ContainsKey(
                    block
                )
            )
            {
                break;
            }

            ulong ownerId =
                blockOwners[block];

            /*
             * Mantenemos desactivado el filtro
             * de propietario que ya tenías.
             */

            if (
                ((1 << block.gameObject.layer) &
                validBlockLayer) == 0
            )
            {
                break;
            }

            FruitData fruitData =
                block.FruitData;

            if (fruitData == null)
                break;

            if (
                fruitData.FruitType !=
                gameManager.TargetSequence[i].FruitType
            )
            {
                break;
            }

            if (i > 0)
            {
                NetworkFruitBlock lowerBlock =
                    orderedBlocks[i - 1];

                if (
                    !AreBlocksStacked(
                        lowerBlock,
                        block
                    )
                )
                {
                    break;
                }
            }

            correctCount++;
        }

        return correctCount;
    }


    // =====================================================
    // ORDENAR POR ALTURA
    // =====================================================

    public List<NetworkFruitBlock>
        GetBlocksOrderedByHeight()
    {
        return fruitBlocksInside
            .Where(
                block => block != null
            )
            .OrderBy(
                block =>
                    block.transform.position.y
            )
            .ToList();
    }


    // =====================================================
    // VALIDAR APILAMIENTO
    // =====================================================

    private bool AreBlocksStacked(
        NetworkFruitBlock lowerBlock,
        NetworkFruitBlock upperBlock
    )
    {
        if (
            lowerBlock == null ||
            upperBlock == null
        )
        {
            return false;
        }

        Collider lowerCollider =
            lowerBlock.GetComponent<Collider>();

        Collider upperCollider =
            upperBlock.GetComponent<Collider>();

        if (
            lowerCollider == null ||
            upperCollider == null
        )
        {
            return false;
        }

        Bounds lowerBounds =
            lowerCollider.bounds;

        Bounds upperBounds =
            upperCollider.bounds;

        float verticalDistance =
            Mathf.Abs(
                upperBounds.min.y -
                lowerBounds.max.y
            );

        if (
            verticalDistance >
            verticalTolerance
        )
        {
            return false;
        }

        bool overlapsX =
            lowerBounds.min.x <
            upperBounds.max.x &&
            lowerBounds.max.x >
            upperBounds.min.x;

        bool overlapsZ =
            lowerBounds.min.z <
            upperBounds.max.z &&
            lowerBounds.max.z >
            upperBounds.min.z;

        return overlapsX &&
               overlapsZ;
    }


    // =====================================================
    // ACTUALIZAR UI
    // =====================================================

    private void UpdateLocalUI(
        int progress
    )
    {
        if (sequenceUI == null)
        {
            sequenceUI =
                FindAnyObjectByType<SequenceUI>();
        }

        if (sequenceUI == null)
            return;

        if (gameManager == null)
            return;

        if (
            gameManager.TargetSequence == null ||
            gameManager.TargetSequence.Count == 0
        )
        {
            return;
        }

        if (
            progress >=
            gameManager.TargetSequence.Count
        )
        {
            sequenceUI.SetCurrentTarget(-1);

            return;
        }

        sequenceUI.SetCurrentTarget(
            progress
        );
    }


    // =====================================================
    // PROPIEDADES
    // =====================================================

    public bool IsHostZone =>
        isHostZone;

    public bool SequenceCompleted =>
        sequenceCompleted;

    public int CurrentProgress =>
        currentProgress;
}
