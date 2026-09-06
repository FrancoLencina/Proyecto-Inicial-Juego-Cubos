using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlsSettingsUI : MonoBehaviour
{
    [Header("Control Buttons")]
    [SerializeField] private Button moveUpButton;
    [SerializeField] private Button moveDownButton;
    [SerializeField] private Button moveLeftButton;
    [SerializeField] private Button moveRightButton;

    [SerializeField] private Button jumpButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button pauseButton;

    [Header("Button Texts")]
    [SerializeField] private TMP_Text moveUpText;
    [SerializeField] private TMP_Text moveDownText;
    [SerializeField] private TMP_Text moveLeftText;
    [SerializeField] private TMP_Text moveRightText;

    [SerializeField] private TMP_Text jumpText;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private TMP_Text sprintText;
    [SerializeField] private TMP_Text pauseText;

    [Header("Other")]
    [SerializeField] private Button resetButton;

    private PlayerControls controls;

    private InputActionRebindingExtensions.RebindingOperation
        rebindingOperation;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        controls = new PlayerControls();

        InputSettings.LoadBindings(
            controls
        );

        controls.Enable();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        SetupButtons();

        UpdateAllBindingTexts();
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private void OnDestroy()
    {
        rebindingOperation?.Dispose();

        controls.Disable();

        controls.Dispose();
    }


    // =========================================================
    // CONFIGURAR BOTONES
    // =========================================================

    private void SetupButtons()
    {
        moveUpButton.onClick.AddListener(
            () => StartRebinding(
                "Move",
                1,
                moveUpText
            )
        );

        moveDownButton.onClick.AddListener(
            () => StartRebinding(
                "Move",
                2,
                moveDownText
            )
        );

        moveLeftButton.onClick.AddListener(
            () => StartRebinding(
                "Move",
                3,
                moveLeftText
            )
        );

        moveRightButton.onClick.AddListener(
            () => StartRebinding(
                "Move",
                4,
                moveRightText
            )
        );

        jumpButton.onClick.AddListener(
            () => StartRebinding(
                "Jump",
                0,
                jumpText
            )
        );

        interactButton.onClick.AddListener(
            () => StartRebinding(
                "Interact",
                0,
                interactText
            )
        );

        sprintButton.onClick.AddListener(
            () => StartRebinding(
                "Sprint",
                0,
                sprintText
            )
        );

        pauseButton.onClick.AddListener(
            () => StartRebinding(
                "Pause",
                0,
                pauseText
            )
        );

        resetButton.onClick.AddListener(
            ResetControls
        );
    }


    // =========================================================
    // INICIAR REASIGNACIÓN
    // =========================================================

    private void StartRebinding(
        string actionName,
        int bindingIndex,
        TMP_Text buttonText
    )
    {
        InputAction action =
            controls.asset.FindAction(
                "Player/" + actionName
            );


        if (action == null)
        {
            Debug.LogError(
                "No se encontró la acción: " +
                actionName
            );

            return;
        }


        buttonText.text =
            "Presiona una tecla...";


        // Desactivamos temporalmente la acción.
        action.Disable();


        rebindingOperation?.Dispose();


        rebindingOperation =
            action.PerformInteractiveRebinding(
                bindingIndex
            )
            .WithControlsExcluding(
                "<Mouse>/position"
            )
            .WithControlsExcluding(
                "<Mouse>/delta"
            )
            .OnCancel(
                operation =>
                {
                    action.Enable();

                    UpdateAllBindingTexts();

                    operation.Dispose();

                    rebindingOperation = null;
                }
            )
            .OnComplete(
                operation =>
                {
                    action.Enable();


                    InputSettings.SaveBindings(
                        controls
                    );


                    UpdateAllBindingTexts();


                    operation.Dispose();

                    rebindingOperation = null;
                }
            );


        rebindingOperation.Start();
    }


    // =========================================================
    // ACTUALIZAR TODOS LOS TEXTOS
    // =========================================================

    private void UpdateAllBindingTexts()
    {
        moveUpText.text =
            GetBindingDisplayName(
                "Move",
                1
            );

        moveDownText.text =
            GetBindingDisplayName(
                "Move",
                2
            );

        moveLeftText.text =
            GetBindingDisplayName(
                "Move",
                3
            );

        moveRightText.text =
            GetBindingDisplayName(
                "Move",
                4
            );

        jumpText.text =
            GetBindingDisplayName(
                "Jump",
                0
            );

        interactText.text =
            GetBindingDisplayName(
                "Interact",
                0
            );

        sprintText.text =
            GetBindingDisplayName(
                "Sprint",
                0
            );

        pauseText.text =
            GetBindingDisplayName(
                "Pause",
                0
            );
    }


    // =========================================================
    // OBTENER NOMBRE DE TECLA
    // =========================================================

    private string GetBindingDisplayName(
        string actionName,
        int bindingIndex
    )
    {
        InputAction action =
            controls.asset.FindAction(
                "Player/" + actionName
            );


        if (action == null)
        {
            return "?";
        }


        return action.GetBindingDisplayString(
            bindingIndex
        );
    }


    // =========================================================
    // RESTABLECER CONTROLES
    // =========================================================

    private void ResetControls()
    {
        InputSettings.ResetBindings(
            controls
        );

        UpdateAllBindingTexts();
    }
}