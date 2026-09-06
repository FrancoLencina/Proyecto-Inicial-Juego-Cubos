using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalTargetZone : MonoBehaviour
{
    public enum PlayerNumber
    {
        Player1,
        Player2
    }

    [Header("Zone Configuration")]
    [SerializeField] private PlayerNumber playerNumber =
        PlayerNumber.Player1;

    [Header("References")]
    [SerializeField] private LocalGameManager gameManager;
    [SerializeField] private LocalSequenceUI sequenceUI;

    [Header("Block Validation")]
    [SerializeField] private LayerMask validBlockLayer;

    [SerializeField] private float verticalTolerance = 0.05f;

    private List<FruitBlock> fruitBlocksInside =
        new List<FruitBlock>();

    private int currentProgress = 0;

    private bool sequenceCompleted = false;

    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager =
                FindAnyObjectByType<LocalGameManager>();
        }

        if (sequenceUI == null)
        {
            LocalSequenceUI[] sequenceUIs =
                FindObjectsByType<LocalSequenceUI>();

            int targetPlayer =
                playerNumber == PlayerNumber.Player1
                    ? 1
                    : 2;

            foreach (LocalSequenceUI ui in sequenceUIs)
            {
                if (ui.PlayerNumber == targetPlayer)
                {
                    sequenceUI = ui;
                    break;
                }
            }
        }

        if (gameManager == null)
        {
            Debug.LogError(
                "[LocalTargetZone] LocalGameManager " +
                "NO encontrado | Zona: " +
                playerNumber
            );
        }
        else
        {
            Debug.Log(
                "[LocalTargetZone] LocalGameManager encontrado | " +
                "Zona: " +
                playerNumber
            );
        }

        if (sequenceUI == null)
        {
            Debug.LogError(
                "[LocalTargetZone] LocalSequenceUI " +
                "NO encontrado | Zona: " +
                playerNumber
            );
        }
        else
        {
            Debug.Log(
                "[LocalTargetZone] LocalSequenceUI encontrado | " +
                "Zona: " +
                playerNumber +
                " | UI: " +
                sequenceUI.gameObject.name
            );
        }
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (gameManager == null)
            return;

        if (gameManager.TargetSequence == null ||
            gameManager.TargetSequence.Count == 0)
        {
            return;
        }

        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        int newProgress =
            GetCorrectProgress();

        if (newProgress != currentProgress)
        {
            currentProgress = newProgress;

            Debug.Log(
                "[LocalTargetZone] Progreso cambiado | " +
                "Jugador: " +
                playerNumber +
                " | Progreso: " +
                currentProgress +
                "/" +
                gameManager.TargetSequence.Count
            );
        }

        // Actualizar siempre el HUD.
        UpdateLocalUI(currentProgress);

        if (sequenceCompleted)
            return;

        if (currentProgress >=
            gameManager.TargetSequence.Count)
        {
            sequenceCompleted = true;

            Debug.Log(
                "[LocalTargetZone] " +
                "SECUENCIA COMPLETADA | " +
                "Jugador: " +
                playerNumber
            );

            NotifyGameManager();
        }
    }

    // =====================================================
    // DETECTAR BLOQUES
    // =====================================================

    private void DetectBlocksInsideZone()
    {
        Collider zoneCollider =
            GetComponent<Collider>();

        if (zoneCollider == null)
            return;

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

        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            FruitBlock fruitBlock =
                collider.GetComponentInParent<FruitBlock>();

            if (fruitBlock == null)
                continue;

            if (!IsValidBlockLayer(fruitBlock))
                continue;

            if (!fruitBlocksInside.Contains(fruitBlock))
            {
                fruitBlocksInside.Add(fruitBlock);

                Debug.Log(
                    "[LocalTargetZone] " +
                    "BLOQUE ENTRÓ A LA ZONA | " +
                    "Jugador: " +
                    playerNumber +
                    " | Fruta: " +
                    fruitBlock.FruitType
                );
            }
        }
    }

    // =====================================================
    // TRIGGER ENTER
    // =====================================================

    private void OnTriggerEnter(Collider other)
    {
        FruitBlock fruitBlock =
            other.GetComponentInParent<FruitBlock>();

        if (fruitBlock == null)
            return;

        if (!IsValidBlockLayer(fruitBlock))
            return;

        if (!fruitBlocksInside.Contains(fruitBlock))
        {
            fruitBlocksInside.Add(fruitBlock);

            Debug.Log(
                "[LocalTargetZone] " +
                "BLOQUE ENTRÓ A LA ZONA | " +
                "Jugador: " +
                playerNumber +
                " | Fruta: " +
                fruitBlock.FruitType
            );
        }

        // Forzar actualización inmediata.
        RefreshProgress();
    }

    // =====================================================
    // TRIGGER EXIT
    // =====================================================

    private void OnTriggerExit(Collider other)
    {
        FruitBlock fruitBlock =
            other.GetComponentInParent<FruitBlock>();

        if (fruitBlock == null)
            return;

        if (fruitBlocksInside.Contains(fruitBlock))
        {
            fruitBlocksInside.Remove(fruitBlock);

            Debug.Log(
                "[LocalTargetZone] " +
                "BLOQUE SALIÓ DE LA ZONA | " +
                "Jugador: " +
                playerNumber +
                " | Fruta: " +
                fruitBlock.FruitType
            );
        }

        sequenceCompleted = false;

        // Forzar actualización inmediata.
        RefreshProgress();
    }

    // =====================================================
    // ACTUALIZAR PROGRESO
    // =====================================================

    private void RefreshProgress()
    {
        if (gameManager == null)
            return;

        if (gameManager.TargetSequence == null ||
            gameManager.TargetSequence.Count == 0)
        {
            return;
        }

        CleanInvalidBlocks();

        DetectBlocksInsideZone();

        int newProgress =
            GetCorrectProgress();

        if (newProgress != currentProgress)
        {
            currentProgress = newProgress;

            Debug.Log(
                "[LocalTargetZone] " +
                "RefreshProgress | " +
                "Jugador: " +
                playerNumber +
                " | Progreso: " +
                currentProgress +
                "/" +
                gameManager.TargetSequence.Count
            );
        }

        UpdateLocalUI(currentProgress);

        if (!sequenceCompleted &&
            currentProgress >=
            gameManager.TargetSequence.Count)
        {
            sequenceCompleted = true;

            Debug.Log(
                "[LocalTargetZone] " +
                "SECUENCIA COMPLETADA | " +
                "Jugador: " +
                playerNumber
            );

            NotifyGameManager();
        }
    }

    // =====================================================
    // VALIDAR LAYER
    // =====================================================

    private bool IsValidBlockLayer(
        FruitBlock fruitBlock
    )
    {
        if (fruitBlock == null)
            return false;

        int blockLayer =
            fruitBlock.gameObject.layer;

        return
            (validBlockLayer.value &
             (1 << blockLayer)) != 0;
    }

    // =====================================================
    // LIMPIAR BLOQUES
    // =====================================================

    private void CleanInvalidBlocks()
    {
        for (
            int i = fruitBlocksInside.Count - 1;
            i >= 0;
            i--
        )
        {
            FruitBlock fruitBlock =
                fruitBlocksInside[i];

            if (fruitBlock == null)
            {
                fruitBlocksInside.RemoveAt(i);
                continue;
            }

            Collider blockCollider =
                fruitBlock.GetComponent<Collider>();

            if (blockCollider == null ||
                !blockCollider.enabled)
            {
                fruitBlocksInside.RemoveAt(i);
            }
        }
    }

    // =====================================================
    // CALCULAR PROGRESO
    // =====================================================

    private int GetCorrectProgress()
    {
        List<FruitBlock> orderedBlocks =
            GetBlocksOrderedByHeight();

        if (gameManager == null ||
            gameManager.TargetSequence == null)
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
            FruitBlock block =
                orderedBlocks[i];

            if (block == null)
                break;

            if (!IsValidBlockLayer(block))
                break;

            if (block.FruitData == null)
                break;

            FruitData targetFruit =
                gameManager.TargetSequence[i];

            if (targetFruit == null)
                break;

            // Comprobar que la fruta sea correcta.
            if (block.FruitType !=
                targetFruit.FruitType)
            {
                break;
            }

            // Comprobar que esté correctamente
            // apilado sobre el bloque anterior.
            if (i > 0)
            {
                FruitBlock lowerBlock =
                    orderedBlocks[i - 1];

                if (!AreBlocksStacked(
                        lowerBlock,
                        block))
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

    public List<FruitBlock>
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
        FruitBlock lowerBlock,
        FruitBlock upperBlock
    )
    {
        if (lowerBlock == null ||
            upperBlock == null)
        {
            return false;
        }

        Collider lowerCollider =
            lowerBlock.GetComponent<Collider>();

        Collider upperCollider =
            upperBlock.GetComponent<Collider>();

        if (lowerCollider == null ||
            upperCollider == null)
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

        if (verticalDistance >
            verticalTolerance)
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

        return overlapsX && overlapsZ;
    }

    // =====================================================
    // ACTUALIZAR UI
    // =====================================================
private void UpdateLocalUI(int progress)
{
    if (sequenceUI == null)
    {
        Debug.LogError(
            "[LocalTargetZone] sequenceUI es NULL | " +
            "Jugador: " +
            playerNumber
        );

        return;
    }

    if (gameManager == null)
    {
        Debug.LogError(
            "[LocalTargetZone] gameManager es NULL | " +
            "Jugador: " +
            playerNumber
        );

        return;
    }

    if (gameManager.TargetSequence == null ||
        gameManager.TargetSequence.Count == 0)
    {
        Debug.LogWarning(
            "[LocalTargetZone] TargetSequence vacía | " +
            "Jugador: " +
            playerNumber
        );

        return;
    }

    if (progress >=
        gameManager.TargetSequence.Count)
    {
        sequenceUI.SetCurrentTarget(-1);
    }
    else
    {
        sequenceUI.SetCurrentTarget(progress);
    }
}

    // =====================================================
    // AVISAR AL GAME MANAGER
    // =====================================================

    private void NotifyGameManager()
    {
        if (gameManager == null)
            return;

        int playerIndex =
            playerNumber == PlayerNumber.Player1
                ? 1
                : 2;

        gameManager.PlayerCompletedSequence(
            playerIndex
        );
    }

    // =====================================================
    // PROPIEDADES
    // =====================================================

    public PlayerNumber ZonePlayer =>
        playerNumber;

    public int CurrentProgress =>
        currentProgress;

    public bool SequenceCompleted =>
        sequenceCompleted;
}