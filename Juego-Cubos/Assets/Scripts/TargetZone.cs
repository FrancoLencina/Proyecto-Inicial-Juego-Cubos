using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetZone : MonoBehaviour
{
    private List<FruitBlock> fruitBlocksInside = new List<FruitBlock>();

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
}