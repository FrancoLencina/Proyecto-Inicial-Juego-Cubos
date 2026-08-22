using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<FruitData> availableFruits;
    [SerializeField] private int sequenceLength = 5;
    [SerializeField] private SequenceUI sequenceUI;

    private List<FruitData> targetSequence;

    private void Start()
    {
        GenerateSequence();
    }

    private void GenerateSequence()
    {
        if (availableFruits.Count < sequenceLength)
        {
            Debug.LogError("No hay suficientes frutas disponibles para generar la secuencia.");
            return;
        }

        targetSequence = new List<FruitData>();

        List<FruitData> availablePool = new List<FruitData>(availableFruits);

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex = Random.Range(0, availablePool.Count);

            FruitData selectedFruit = availablePool[randomIndex];

            targetSequence.Add(selectedFruit);

            availablePool.RemoveAt(randomIndex);
        }

        Debug.Log("Secuencia generada:");

        foreach (FruitData fruit in targetSequence)
        {
            Debug.Log(fruit.DisplayName);
        }

        sequenceUI.DisplaySequence(targetSequence.ToArray());
    }
}