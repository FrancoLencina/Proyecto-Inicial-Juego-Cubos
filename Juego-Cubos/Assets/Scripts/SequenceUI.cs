using UnityEngine;
using UnityEngine.UI;

public class SequenceUI : MonoBehaviour
{
    [SerializeField] private GameObject fruitImagePrefab;
    [SerializeField] private Transform container;

    public void DisplaySequence(FruitData[] sequence)
    {
        ClearSequence();

        for (int i = sequence.Length - 1; i >= 0; i--)
        {
            GameObject fruitObject = Instantiate(
                fruitImagePrefab,
                container
            );

            Image image =
                fruitObject.GetComponent<Image>();

            image.sprite = sequence[i].Image;
        }
    }

    private void ClearSequence()
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}