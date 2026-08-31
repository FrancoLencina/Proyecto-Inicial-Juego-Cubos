using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkBlockSpawner : NetworkBehaviour
{
    [Header("Network Block")]
    [SerializeField] private NetworkFruitBlock networkFruitBlockPrefab;

    [Header("Fruits")]
    [SerializeField] private List<FruitData> availableFruits;

    [SerializeField] private int blocksPerFruit = 2;

    [Header("Spawn Area")]
    [SerializeField] private Collider spawnArea;

    public override void OnNetworkSpawn()
    {
        // Solo el Host/Servidor genera los bloques.
        if (!IsServer)
            return;

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

        Debug.Log(
            "[NetworkBlockSpawner] Generando "
            + fruit.DisplayName
            + " en posición: "
            + spawnPosition
        );

        NetworkFruitBlock newBlock = Instantiate(
            networkFruitBlockPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // Primero registramos el objeto en la red.
        newBlock.NetworkObject.Spawn();

        // Después asignamos la fruta.
        newBlock.SetFruitData(fruit);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnArea.bounds;

        float randomX = Random.Range(
            bounds.min.x,
            bounds.max.x
        );

        float randomZ = Random.Range(
            bounds.min.z,
            bounds.max.z
        );

        float spawnY = bounds.max.y + 1f;

        float blockHeight = 1f;

        while (Physics.CheckBox(
            new Vector3(randomX, spawnY, randomZ),
            new Vector3(0.45f, 0.45f, 0.45f)
        ))
        {
            spawnY += blockHeight;
        }

        return new Vector3(
            randomX,
            spawnY,
            randomZ
        );
    }
}