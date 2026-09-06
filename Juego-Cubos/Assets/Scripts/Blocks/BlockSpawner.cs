using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FruitBlock fruitBlockPrefab;
    [SerializeField] private List<FruitData> availableFruits;
    [SerializeField] private Collider spawnArea;

    [Header("Spawn")]
    [SerializeField] private int blocksPerFruit = 2;

    [SerializeField] private float spawnHeight = 1f;

    [Header("Collision")]
    [SerializeField] private float blockCheckRadius = 0.45f;

    [SerializeField] private int maxSpawnAttempts = 30;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        SpawnBlocks();
    }


    // =========================================================
    // SPAWN BLOCKS
    // =========================================================

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


    // =========================================================
    // SPAWN BLOCK
    // =========================================================

    private void SpawnBlock(FruitData fruit)
    {
        Vector3 spawnPosition;

        bool foundPosition =
            TryGetRandomSpawnPosition(
                out spawnPosition
            );

        if (!foundPosition)
        {
            Debug.LogWarning(
                "No se encontró una posición libre para " +
                fruit.DisplayName
            );

            return;
        }


        Debug.Log(
            "Generando " +
            fruit.DisplayName +
            " en posición: " +
            spawnPosition
        );


        FruitBlock newBlock =
            Instantiate(
                fruitBlockPrefab,
                spawnPosition,
                Quaternion.identity
            );


        newBlock.SetFruitData(
            fruit
        );
    }


    // =========================================================
    // BUSCAR POSICIÓN
    // =========================================================

    private bool TryGetRandomSpawnPosition(
        out Vector3 spawnPosition
    )
    {
        Bounds bounds =
            spawnArea.bounds;


        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            // =================================================
            // POSICIÓN ALEATORIA
            // =================================================

            float randomX =
                Random.Range(
                    bounds.min.x,
                    bounds.max.x
                );


            float randomZ =
                Random.Range(
                    bounds.min.z,
                    bounds.max.z
                );


            // =================================================
            // ALTURA
            // =================================================
            //
            // Siempre empezamos desde el suelo.
            //
            // No subimos Y buscando espacio.
            //
            // =================================================

            float randomY =
                bounds.max.y +
                spawnHeight;


            Vector3 candidate =
                new Vector3(
                    randomX,
                    randomY,
                    randomZ
                );


            // =================================================
            // COMPROBAR SI ESTÁ LIBRE
            // =================================================

            bool occupied =
                Physics.CheckBox(
                    candidate,
                    new Vector3(
                        blockCheckRadius,
                        blockCheckRadius,
                        blockCheckRadius
                    ),
                    Quaternion.identity,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );


            // =================================================
            // POSICIÓN LIBRE
            // =================================================

            if (!occupied)
            {
                spawnPosition =
                    candidate;

                return true;
            }
        }


        // =====================================================
        // NO SE ENCONTRÓ POSICIÓN
        // =====================================================

        spawnPosition =
            Vector3.zero;

        return false;
    }
}