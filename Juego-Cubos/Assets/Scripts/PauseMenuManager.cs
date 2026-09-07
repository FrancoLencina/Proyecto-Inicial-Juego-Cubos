using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;


    private PlayerControls controls;

    private bool isPaused;
    private PlayerCamera playerCamera;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        controls = new PlayerControls();

        InputSettings.LoadBindings(
            controls
        );
    }


    // =========================================================
    // ON ENABLE
    // =========================================================

    private void OnEnable()
    {
        controls.Enable();
    }


    // =========================================================
    // ON DISABLE
    // =========================================================

    private void OnDisable()
    {
        controls.Disable();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        playerCamera =
            FindAnyObjectByType<PlayerCamera>();


        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        continueButton.onClick.AddListener(
            ResumeGame
        );

        restartButton.onClick.AddListener(
            RestartGame
        );

        mainMenuButton.onClick.AddListener(
            ReturnToMainMenu
        );
    }  


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (controls.Player.Pause.WasPressedThisFrame())
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }


    // =========================================================
    // PAUSAR
    // =========================================================

    public void PauseGame()
    {
        isPaused = true;

        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        if (playerCamera != null)
        {
            playerCamera.SetControlsEnabled(false);
        }

        SoundManager.Instance.PlayPauseOpen();
    }


    // =========================================================
    // CONTINUAR
    // =========================================================

    public void ResumeGame()
    {
        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        if (playerCamera != null)
        {
            playerCamera.SetControlsEnabled(true);
        }

        SoundManager.Instance.PlayPauseClose();
    }


    // =========================================================
    // REINICIAR
    // =========================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    // =========================================================
    // VOLVER AL MENÚ PRINCIPAL
    // =========================================================

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            "MainMenuScene"
        );
    }


    // =========================================================
    // ON DESTROY
    // =========================================================

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        controls?.Disable();

        controls?.Dispose();
    }
}