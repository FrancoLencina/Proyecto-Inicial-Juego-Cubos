using UnityEngine;

public class FruitBlock : MonoBehaviour
{
    [SerializeField] private FruitData fruitData;

    private Renderer blockRenderer;

    public FruitData FruitData => fruitData;
    public FruitType FruitType => fruitData.FruitType;

    private void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
        ApplyFruitData();
    }

    public void SetFruitData(FruitData newFruitData)
    {
        fruitData = newFruitData;
        ApplyFruitData();
    }

    private void ApplyFruitData()
    {
        if (fruitData == null || blockRenderer == null)
            return;

        blockRenderer.material = fruitData.Material;
    }
}