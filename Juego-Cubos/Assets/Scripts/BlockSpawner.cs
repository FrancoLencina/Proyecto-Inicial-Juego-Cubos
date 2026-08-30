using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [SerializeField] private FruitBlock fruitBlockPrefab;
    [SerializeField] private List<FruitData> availableFruits;

    [SerializeField] private int blocksPerFruit = 2;

    [SerializeField] private Collider spawnArea;

    private void Start()
    {
        SpawnBlocks();
    }

    private void SpawnBlocks()
    {
        foreach (FruitData fruit in availableFruits)
        {
            for (int i = 0; i < blocksPerFruit; i++)
            {
                SpawnBlock(fruit);
            }
        }
    }

    private void SpawnBlock(FruitData fruit)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        Debug.Log("Generando " + fruit.DisplayName + " en posici�n: " + spawnPosition);

        FruitBlock newBlock = Instantiate(
            fruitBlockPrefab,
            spawnPosition,
            Quaternion.identity
        );

        newBlock.SetFruitData(fruit);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnArea.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        float spawnY = bounds.max.y + 1f;

        float blockHeight = 1f;

        while (Physics.CheckBox(
            new Vector3(randomX, spawnY, randomZ),
            new Vector3(0.45f, 0.45f, 0.45f)
        ))
        {
            spawnY += blockHeight;
        }

        return new Vector3(randomX, spawnY, randomZ);
    }
}