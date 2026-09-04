using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask validBlockLayer;
    [SerializeField] private float verticalTolerance = 0.05f;

    [Header("Victory Check")]
    [SerializeField] private float victoryDelay = 1.5f;

    private List<FruitBlock> fruitBlocksInside =
        new List<FruitBlock>();

    private int currentProgress = -1;

    private bool sequenceCompleted = false;
    private bool checkingVictory = false;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (sequenceCompleted)
            return;

        CleanInvalidBlocks();

        UpdateSequenceProgress();

        /*
         * Si la pila parece correcta, NO ganamos inmediatamente.
         *
         * Esperamos un segundo para asegurarnos de que los bloques
         * realmente permanecen apilados.
         */
        if (IsCorrectStack() &&
            !checkingVictory)
        {
            checkingVictory = true;

            StartCoroutine(
                CheckVictoryAfterDelay()
            );
        }
    }


    // =========================================================
    // ESPERAR ANTES DE CONFIRMAR VICTORIA
    // =========================================================

    private IEnumerator CheckVictoryAfterDelay()
    {
        Debug.Log(
            "[TargetZone] Pila correcta. Esperando " +
            victoryDelay +
            " segundos para confirmar..."
        );

        yield return new WaitForSeconds(
            victoryDelay
        );

        /*
         * Volvemos a limpiar la lista por si algún bloque
         * salió de la zona durante este segundo.
         */
        CleanInvalidBlocks();

        /*
         * VOLVEMOS A COMPROBAR TODA LA PILA.
         *
         * Si el bloque se cayó, esto será false
         * y NO habrá victoria.
         */
        if (IsCorrectStack())
        {
            sequenceCompleted = true;

            Debug.Log(
                "¡VICTORIA! La pila permaneció completa durante " +
                victoryDelay +
                " segundo."
            );

            if (gameManager != null)
            {
                gameManager.CompleteGame();
            }
        }
        else
        {
            Debug.Log(
                "[TargetZone] La pila se desarmó durante la espera. " +
                "Victoria cancelada."
            );
        }

        checkingVictory = false;
    }


    // =========================================================
    // LIMPIAR BLOQUES INVÁLIDOS
    // =========================================================

    private void CleanInvalidBlocks()
    {
        for (int i = fruitBlocksInside.Count - 1; i >= 0; i--)
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

                Debug.Log(
                    "FruitBlock salió de la zona: " +
                    fruitBlock.FruitData.DisplayName
                );
            }
        }
    }


    // =========================================================
    // ACTUALIZAR PROGRESO
    // =========================================================

    private void UpdateSequenceProgress()
    {
        if (gameManager == null ||
            gameManager.TargetSequence == null)
        {
            return;
        }

        int newProgress =
            GetCorrectProgress();

        if (newProgress == currentProgress)
            return;

        currentProgress = newProgress;

        if (newProgress <
            gameManager.TargetSequence.Count)
        {
            gameManager.UpdateCurrentTarget(
                newProgress
            );

            Debug.Log(
                "Nuevo objetivo: " +
                gameManager
                    .TargetSequence[newProgress]
                    .DisplayName
            );
        }
    }


    // =========================================================
    // OBTENER PROGRESO
    // =========================================================

    private int GetCorrectProgress()
    {
        List<FruitBlock> orderedBlocks =
            GetBlocksOrderedByHeight();

        int correctCount = 0;

        for (int i = 0;
             i < orderedBlocks.Count;
             i++)
        {
            if (i >= gameManager.TargetSequence.Count)
                break;

            FruitBlock block =
                orderedBlocks[i];

            if (((1 << block.gameObject.layer) &
                 validBlockLayer) == 0)
            {
                break;
            }

            FruitType blockFruitType =
                block.FruitData.FruitType;

            FruitType targetFruitType =
                gameManager
                    .TargetSequence[i]
                    .FruitType;

            if (blockFruitType != targetFruitType)
            {
                break;
            }

            if (i > 0)
            {
                FruitBlock lowerBlock =
                    orderedBlocks[i - 1];

                if (!AreBlocksStacked(
                    lowerBlock,
                    block
                ))
                {
                    break;
                }
            }

            correctCount++;
        }

        return correctCount;
    }


    // =========================================================
    // TRIGGER ENTER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        FruitBlock fruitBlock =
            other.GetComponent<FruitBlock>();

        if (fruitBlock != null &&
            !fruitBlocksInside.Contains(fruitBlock))
        {
            fruitBlocksInside.Add(fruitBlock);

            Debug.Log(
                "FruitBlock entró en la zona: " +
                fruitBlock.FruitData.DisplayName
            );
        }
    }


    // =========================================================
    // TRIGGER EXIT
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        FruitBlock fruitBlock =
            other.GetComponent<FruitBlock>();

        if (fruitBlock != null)
        {
            fruitBlocksInside.Remove(fruitBlock);

            Debug.Log(
                "FruitBlock salió de la zona: " +
                fruitBlock.FruitData.DisplayName
            );
        }
    }


    // =========================================================
    // ORDENAR BLOQUES
    // =========================================================

    public List<FruitBlock> GetBlocksOrderedByHeight()
    {
        return fruitBlocksInside
            .Where(block => block != null)
            .OrderBy(
                block => block.transform.position.y
            )
            .ToList();
    }


    // =========================================================
    // COMPROBAR PILA
    // =========================================================

    private bool IsCorrectStack()
    {
        List<FruitBlock> orderedBlocks =
            GetBlocksOrderedByHeight();

        if (gameManager == null ||
            gameManager.TargetSequence == null)
        {
            return false;
        }

        if (orderedBlocks.Count !=
            gameManager.TargetSequence.Count)
        {
            return false;
        }

        for (int i = 0;
             i < orderedBlocks.Count;
             i++)
        {
            FruitBlock block =
                orderedBlocks[i];

            if (((1 << block.gameObject.layer) &
                 validBlockLayer) == 0)
            {
                return false;
            }

            FruitType blockFruitType =
                block.FruitData.FruitType;

            FruitType targetFruitType =
                gameManager
                    .TargetSequence[i]
                    .FruitType;

            if (blockFruitType != targetFruitType)
            {
                return false;
            }

            if (i > 0)
            {
                FruitBlock lowerBlock =
                    orderedBlocks[i - 1];

                if (!AreBlocksStacked(
                    lowerBlock,
                    block
                ))
                {
                    return false;
                }
            }
        }

        return true;
    }


    // =========================================================
    // COMPROBAR SI ESTÁN APILADOS
    // =========================================================

    private bool AreBlocksStacked(
        FruitBlock lowerBlock,
        FruitBlock upperBlock
    )
    {
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
}
