using UnityEngine;

public class FruitBlock : MonoBehaviour
{
    [SerializeField] private FruitData fruitData;

    public FruitData FruitData => fruitData;
    public FruitType FruitType => fruitData.FruitType;
}