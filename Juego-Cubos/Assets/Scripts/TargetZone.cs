using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TargetZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask validBlockLayer;
    [SerializeField] private float verticalTolerance = 0.05f;

    private List<FruitBlock> fruitBlocksInside =
        new List<FruitBlock>();

    private int currentProgress = -1;

    private void Update()
    {
        CleanInvalidBlocks();

        UpdateSequenceProgress();

        if (IsCorrectStack())
        {
            Debug.Log("¡Apilamiento correcto!");

            SceneManager.LoadScene("EndScene");
        }
    }

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

    private void UpdateSequenceProgress()
    {
        if (gameManager == null ||
            gameManager.TargetSequence == null)
        {
            return;
        }

        int newProgress =
            GetCorrectProgress();

        /*
         * Solo actualizamos el HUD cuando
         * realmente cambió el progreso.
         */
        if (newProgress == currentProgress)
            return;

        currentProgress = newProgress;

        /*
         * newProgress representa cuántos bloques
         * correctos ya tenemos.
         *
         * Ejemplo:
         *
         * 0 correctos → objetivo índice 0
         * 1 correcto  → objetivo índice 1
         * 2 correctos → objetivo índice 2
         */

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

            /*
             * Verificamos que sea un bloque válido.
             */
            if (((1 << block.gameObject.layer) &
                 validBlockLayer) == 0)
            {
                break;
            }

            /*
             * Verificamos que la fruta sea la correcta.
             */
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

            /*
             * A partir del segundo bloque verificamos
             * que realmente esté apilado sobre el anterior.
             */
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

    public List<FruitBlock> GetBlocksOrderedByHeight()
    {
        return fruitBlocksInside
            .Where(block => block != null)
            .OrderBy(
                block => block.transform.position.y
            )
            .ToList();
    }

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