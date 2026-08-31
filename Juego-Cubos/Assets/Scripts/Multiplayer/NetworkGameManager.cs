using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Fruit Configuration")]
    [SerializeField] private List<FruitData> availableFruits;
    [SerializeField] private int sequenceLength = 5;

    private List<FruitData> targetSequence;

    private NetworkList<int> networkSequence;

    public IReadOnlyList<FruitData> TargetSequence => targetSequence;

    private void Awake()
    {
        networkSequence = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        networkSequence.OnListChanged += OnSequenceChanged;

        Debug.Log(
            "[NetworkGameManager] Network Spawn. IsServer: "
            + IsServer
            + " | IsClient: "
            + IsClient
        );

        if (IsServer)
        {
            GenerateSequence();
        }

        // Intentamos encontrar la UI por si ya existe.
        TryUpdateSequenceUI();
    }

    public override void OnNetworkDespawn()
    {
        networkSequence.OnListChanged -= OnSequenceChanged;
    }

    private void GenerateSequence()
    {
        if (availableFruits == null || availableFruits.Count < sequenceLength)
        {
            Debug.LogError(
                "[NetworkGameManager] No hay suficientes frutas disponibles."
            );

            return;
        }

        List<FruitData> availablePool =
            new List<FruitData>(availableFruits);

        networkSequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex =
                Random.Range(0, availablePool.Count);

            FruitData selectedFruit =
                availablePool[randomIndex];

            int fruitID =
                availableFruits.IndexOf(selectedFruit);

            networkSequence.Add(fruitID);

            availablePool.RemoveAt(randomIndex);
        }

        Debug.Log("[NetworkGameManager] Secuencia generada por el HOST:");

        foreach (int fruitID in networkSequence)
        {
            Debug.Log(
                "[NetworkGameManager] "
                + availableFruits[fruitID].DisplayName
            );
        }

        UpdateLocalSequence();
    }

    private void OnSequenceChanged(NetworkListEvent<int> changeEvent)
    {
        Debug.Log(
            "[NetworkGameManager] La secuencia cambió. Elementos: "
            + networkSequence.Count
        );

        UpdateLocalSequence();
    }

    private void UpdateLocalSequence()
    {
        if (networkSequence.Count == 0)
            return;

        targetSequence = new List<FruitData>();

        foreach (int fruitID in networkSequence)
        {
            if (fruitID < 0 || fruitID >= availableFruits.Count)
            {
                Debug.LogError(
                    "[NetworkGameManager] ID de fruta inválido: "
                    + fruitID
                );

                continue;
            }

            targetSequence.Add(availableFruits[fruitID]);
        }

        TryUpdateSequenceUI();
    }

    private void TryUpdateSequenceUI()
{
    if (networkSequence.Count == 0)
        return;

    SequenceUI sequenceUI =
        FindAnyObjectByType<SequenceUI>();

    if (sequenceUI == null)
    {
        Debug.Log(
            "[NetworkGameManager] SequenceUI todavía no está disponible."
        );

        return;
    }

    if (targetSequence == null || targetSequence.Count == 0)
    {
        targetSequence = new List<FruitData>();

        foreach (int fruitID in networkSequence)
        {
            if (fruitID >= 0 && fruitID < availableFruits.Count)
            {
                targetSequence.Add(
                    availableFruits[fruitID]
                );
            }
        }
    }

    sequenceUI.DisplaySequence(
        targetSequence.ToArray()
    );
}
}