using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "Game/Fruit Data")]
public class FruitData : ScriptableObject
{
    [SerializeField] private FruitType fruitType;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite image;

    public FruitType FruitType => fruitType;
    public string DisplayName => displayName;
    public Sprite Image => image;
}