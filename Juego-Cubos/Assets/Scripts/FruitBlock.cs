using UnityEngine;

public class FruitBlock : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

<<<<<<< Updated upstream
    // Update is called once per frame
    void Update()
    {
        
    }
}
=======
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
>>>>>>> Stashed changes
