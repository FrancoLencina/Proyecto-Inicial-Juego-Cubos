using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SequenceUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject fruitImagePrefab;
    [SerializeField] private Transform container;

    [Header("Highlight")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float highlightedScale = 1.2f;

    [Header("Border")]
    [SerializeField] private Color borderColor = Color.yellow;
    [SerializeField] private Vector2 borderSize = new Vector2(4f, -4f);

    private List<GameObject> fruitObjects = new List<GameObject>();

    // 0 = primer bloque que debe colocarse.
    private int currentTargetIndex = 0;

    public void DisplaySequence(FruitData[] sequence)
    {
        ClearSequence();

        fruitObjects.Clear();

        // Se crea al revés visualmente:
        // Arriba = último bloque
        // Abajo = primer bloque
        for (int i = sequence.Length - 1; i >= 0; i--)
        {
            GameObject fruitObject = Instantiate(
                fruitImagePrefab,
                container
            );

            Image image = fruitObject.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = sequence[i].Image;
            }

            fruitObjects.Add(fruitObject);
        }

        currentTargetIndex = 0;

        UpdateHighlight();
    }

    public void SetCurrentTarget(int targetIndex)
    {
        currentTargetIndex = targetIndex;

        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        if (fruitObjects.Count == 0)
            return;

        for (int i = 0; i < fruitObjects.Count; i++)
        {
            GameObject fruitObject = fruitObjects[i];

            if (fruitObject == null)
                continue;

            /*
             * Como la UI está invertida:
             *
             * fruitObjects[0] = último de la secuencia
             * fruitObjects[último] = primero de la secuencia
             */
            int realSequenceIndex =
                fruitObjects.Count - 1 - i;

            bool isCurrentTarget =
                realSequenceIndex == currentTargetIndex;

            // Solo agrandamos el bloque actual.
            fruitObject.transform.localScale =
                isCurrentTarget
                    ? Vector3.one * highlightedScale
                    : Vector3.one * normalScale;

            // El color de la fruta NO se modifica.
            UpdateBorder(
                fruitObject,
                isCurrentTarget
            );
        }
    }

    private void UpdateBorder(
        GameObject fruitObject,
        bool active
    )
    {
        Outline outline =
            fruitObject.GetComponent<Outline>();

        if (outline == null)
        {
            outline = fruitObject.AddComponent<Outline>();

            outline.effectColor = borderColor;
            outline.effectDistance = borderSize;
        }

        outline.enabled = active;
    }

    private void ClearSequence()
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}