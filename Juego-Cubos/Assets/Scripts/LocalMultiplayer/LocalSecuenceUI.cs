using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalSequenceUI : MonoBehaviour
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

    [Header("Local Player")]
    [SerializeField] private int playerNumber = 1;

    private List<GameObject> fruitObjects =
        new List<GameObject>();

    private int currentTargetIndex = 0;

    // =====================================================
    // MOSTRAR SECUENCIA
    // =====================================================

    public void DisplaySequence(FruitData[] sequence)
    {
        Debug.Log(
            "[LocalSequenceUI] DisplaySequence llamado | " +
            "Player: " +
            playerNumber
        );

        if (container == null)
        {
            Debug.LogError(
                "[LocalSequenceUI] Container no está asignado | " +
                "Player: " +
                playerNumber
            );

            return;
        }

        if (fruitImagePrefab == null)
        {
            Debug.LogError(
                "[LocalSequenceUI] Fruit Image Prefab no está asignado | " +
                "Player: " +
                playerNumber
            );

            return;
        }

        if (sequence == null)
        {
            Debug.LogError(
                "[LocalSequenceUI] Sequence es NULL | " +
                "Player: " +
                playerNumber
            );

            ClearSequence();
            return;
        }

        if (sequence.Length == 0)
        {
            Debug.LogWarning(
                "[LocalSequenceUI] Sequence vacía | " +
                "Player: " +
                playerNumber
            );

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

        for (int i = sequence.Length - 1; i >= 0; i--)
        {
            if (sequence[i] == null)
            {
                Debug.LogWarning(
                    "[LocalSequenceUI] FruitData NULL en índice " +
                    i +
                    " | Player: " +
                    playerNumber
                );

                continue;
            }

            GameObject fruitObject =
                Instantiate(
                    fruitImagePrefab,
                    container
                );

            fruitObject.name =
                "Fruit_" +
                sequence[i].FruitType;

            Image image =
                fruitObject.GetComponent<Image>();

            if (image != null)
            {
                image.sprite =
                    sequence[i].Image;
            }
            else
            {
                Debug.LogWarning(
                    "[LocalSequenceUI] El prefab no tiene Image | " +
                    "Player: " +
                    playerNumber
                );
            }

            fruitObject.transform.localScale =
                Vector3.one * normalScale;

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

            outline.enabled = false;

            fruitObjects.Add(
                fruitObject
            );
        }

        currentTargetIndex = 0;

        UpdateHighlight();

        Debug.Log(
            "[LocalSequenceUI] Player " +
            playerNumber +
            " | Secuencia mostrada | " +
            "Cantidad: " +
            fruitObjects.Count
        );
    }

    // =====================================================
    // CAMBIAR OBJETIVO ACTUAL
    // =====================================================

    public void SetCurrentTarget(int targetIndex)
    {
        if (currentTargetIndex == targetIndex)
            return;

        Debug.Log(
            "[LocalSequenceUI] SetCurrentTarget | " +
            "Player: " +
            playerNumber +
            " | Anterior: " +
            currentTargetIndex +
            " | Nuevo: " +
            targetIndex +
            " | Objetos: " +
            fruitObjects.Count
        );

        currentTargetIndex =
            targetIndex;

        UpdateHighlight();
    }

    // =====================================================
    // ACTUALIZAR HIGHLIGHT
    // =====================================================

    private void UpdateHighlight()
    {
        if (fruitObjects == null)
            return;

        if (fruitObjects.Count == 0)
        {
            Debug.LogWarning(
                "[LocalSequenceUI] UpdateHighlight sin objetos | " +
                "Player: " +
                playerNumber
            );

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
             * La lista visual está invertida.
             *
             * Ejemplo:
             *
             * Secuencia real:
             * 0 = Apple
             * 1 = Banana
             * 2 = Carrot
             * 3 = Watermelon
             * 4 = Tomato
             *
             * Visual:
             * i = 0 -> Tomato
             * i = 1 -> Watermelon
             * i = 2 -> Carrot
             * i = 3 -> Banana
             * i = 4 -> Apple
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
                    ? Vector3.one * highlightedScale
                    : Vector3.one * normalScale;

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
        {
            fruitObjects.Clear();
            currentTargetIndex = 0;
            return;
        }

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

    public int PlayerNumber =>
        playerNumber;
}