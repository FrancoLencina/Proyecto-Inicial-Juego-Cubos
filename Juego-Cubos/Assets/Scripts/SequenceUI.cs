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
    [SerializeField] private Vector2 borderSize =
        new Vector2(4f, -4f);

    private List<GameObject> fruitObjects =
        new List<GameObject>();

    private int currentTargetIndex = 0;

    // =====================================================
    // MOSTRAR SECUENCIA
    // =====================================================

    public void DisplaySequence(
        FruitData[] sequence
    )
    {
        if (container == null)
        {
            Debug.LogError(
                "[SequenceUI] Container no está asignado."
            );

            return;
        }

        if (fruitImagePrefab == null)
        {
            Debug.LogError(
                "[SequenceUI] Fruit Image Prefab no está asignado."
            );

            return;
        }

        if (
            sequence == null ||
            sequence.Length == 0
        )
        {
            ClearSequence();
            return;
        }

        ClearSequence();

        fruitObjects.Clear();

        /*
         * La UI se muestra invertida:
         *
         * Arriba    = último objetivo
         * ...
         * Abajo     = primer objetivo
         */

        for (
            int i = sequence.Length - 1;
            i >= 0;
            i--
        )
        {
            if (sequence[i] == null)
                continue;

            GameObject fruitObject =
                Instantiate(
                    fruitImagePrefab,
                    container
                );

            Image image =
                fruitObject.GetComponent<Image>();

            if (image != null)
            {
                image.sprite =
                    sequence[i].Image;
            }

            fruitObject.transform.localScale =
                Vector3.one *
                normalScale;

            Outline outline =
                fruitObject.GetComponent<Outline>();

            if (outline == null)
            {
                outline =
                    fruitObject.AddComponent<Outline>();
            }

            outline.effectColor =
                borderColor;

            outline.effectDistance =
                borderSize;

            outline.enabled =
                false;

            fruitObjects.Add(
                fruitObject
            );
        }

        currentTargetIndex = 0;

        UpdateHighlight();

        Debug.Log(
            "[SequenceUI] Secuencia mostrada en el HUD | " +
            "Cantidad: " +
            fruitObjects.Count
        );
    }

    // =====================================================
    // CAMBIAR OBJETIVO ACTUAL
    // =====================================================

    public void SetCurrentTarget(
        int targetIndex
    )
    {
        currentTargetIndex =
            targetIndex;

        UpdateHighlight();
    }

    // =====================================================
    // ACTUALIZAR HIGHLIGHT
    // =====================================================

    private void UpdateHighlight()
    {
        if (
            fruitObjects.Count == 0
        )
        {
            return;
        }

        for (
            int i = 0;
            i < fruitObjects.Count;
            i++
        )
        {
            GameObject fruitObject =
                fruitObjects[i];

            if (fruitObject == null)
                continue;

            /*
             * Como la lista visual está invertida,
             * convertimos la posición visual a la
             * posición real de la secuencia.
             */

            int realSequenceIndex =
                fruitObjects.Count -
                1 -
                i;

            bool isCurrentTarget =
                currentTargetIndex >= 0 &&
                realSequenceIndex ==
                currentTargetIndex;

            fruitObject.transform.localScale =
                isCurrentTarget
                    ? Vector3.one *
                      highlightedScale
                    : Vector3.one *
                      normalScale;

            UpdateBorder(
                fruitObject,
                isCurrentTarget
            );
        }
    }

    // =====================================================
    // BORDE
    // =====================================================

    private void UpdateBorder(
        GameObject fruitObject,
        bool active
    )
    {
        if (fruitObject == null)
            return;

        Outline outline =
            fruitObject.GetComponent<Outline>();

        if (outline == null)
        {
            outline =
                fruitObject.AddComponent<Outline>();
        }

        outline.effectColor =
            borderColor;

        outline.effectDistance =
            borderSize;

        outline.enabled =
            active;
    }

    // =====================================================
    // LIMPIAR
    // =====================================================

    public void ClearSequence()
    {
        if (container == null)
            return;

        for (
            int i =
                container.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                container.GetChild(i).gameObject
            );
        }

        fruitObjects.Clear();

        currentTargetIndex = 0;
    }

    // =====================================================
    // PROPIEDADES
    // =====================================================

    public int CurrentTargetIndex =>
        currentTargetIndex;

    public int SequenceCount =>
        fruitObjects.Count;
}