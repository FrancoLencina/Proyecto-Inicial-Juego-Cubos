using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button multiPlayerButton;
    [SerializeField] private Button settingsButton;


    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;


    [Header("Settings")]
    [SerializeField] private Button backButton;


    private bool alreadyLoaded = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;


        // Estado inicial.
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }


        // Validar que no se haya cargado antes.
        if (!alreadyLoaded)
        {
            alreadyLoaded = true;

            if (singlePlayerButton != null)
            {
                singlePlayerButton.onClick.AddListener(
                    StartSinglePlayer
                );
            }

            if (multiPlayerButton != null)
            {
                multiPlayerButton.onClick.AddListener(
                    StartMultiPlayer
                );
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(
                    OpenSettings
                );
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(
                    CloseSettings
                );
            }
        }
    }


    // =========================================================
    // ABRIR CONFIGURACIONES
    // =========================================================

    private void OpenSettings()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }


    // =========================================================
    // CERRAR CONFIGURACIONES
    // =========================================================

    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }


    // =========================================================
    // INICIAR MODO CONTRARRELOJ / SINGLEPLAYER
    // =========================================================

    private void StartSinglePlayer()
    {
        SceneManager.LoadScene("JuanScene");
    }


    // =========================================================
    // INICIAR MODO VERSUS / MULTIPLAYER
    // =========================================================

    private void StartMultiPlayer()
    {
        SceneManager.LoadScene("MultiplayerScene");
    }
}