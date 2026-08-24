using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask validBlockLayer;

    private List<FruitBlock> fruitBlocksInside = new List<FruitBlock>();
    private bool stackWasCorrect = false;

    private void Update()
    {
        for (int i = fruitBlocksInside.Count - 1; i >= 0; i--)
        {
            FruitBlock fruitBlock = fruitBlocksInside[i];

            if (fruitBlock == null)
            {
                fruitBlocksInside.RemoveAt(i);
                continue;
            }

            Collider blockCollider = fruitBlock.GetComponent<Collider>();

            if (blockCollider == null || !blockCollider.enabled)
            {
                fruitBlocksInside.RemoveAt(i);

                Debug.Log("FruitBlock salió de la zona: " + fruitBlock.FruitData.DisplayName);
            }
        }

        bool stackIsCorrect = IsCorrectStack();

        if (stackIsCorrect && !stackWasCorrect)
        {
            Debug.Log("¡Apilamiento correcto!");
        }

        stackWasCorrect = stackIsCorrect;
    }

    private void OnTriggerEnter(Collider other)
    {
        FruitBlock fruitBlock = other.GetComponent<FruitBlock>();

        if (fruitBlock != null && !fruitBlocksInside.Contains(fruitBlock))
        {
            fruitBlocksInside.Add(fruitBlock);

            Debug.Log("FruitBlock entró en la zona: " + fruitBlock.FruitData.DisplayName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FruitBlock fruitBlock = other.GetComponent<FruitBlock>();

        if (fruitBlock != null)
        {
            fruitBlocksInside.Remove(fruitBlock);

            Debug.Log("FruitBlock salió de la zona: " + fruitBlock.FruitData.DisplayName);
        }
    }

    public List<FruitBlock> GetBlocksOrderedByHeight()
    {
        return fruitBlocksInside
            .OrderBy(block => block.transform.position.y)
            .ToList();
    }

    private bool IsCorrectStack()
    {
        List<FruitBlock> orderedBlocks = GetBlocksOrderedByHeight();

        if (gameManager == null || gameManager.TargetSequence == null)
        {
            return false;
        }

        if (orderedBlocks.Count != gameManager.TargetSequence.Count)
        {
            return false;
        }

        for (int i = 0; i < orderedBlocks.Count; i++)
        {
            FruitBlock block = orderedBlocks[i];

            if (((1 << block.gameObject.layer) & validBlockLayer) == 0)
            {
                return false;
            }

            FruitType blockFruitType = block.FruitData.FruitType;
            FruitType targetFruitType = gameManager.TargetSequence[i].FruitType;

            if (blockFruitType != targetFruitType)
            {
                return false;
            }
        }

        return true;
    }
}