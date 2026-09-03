using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
[Header("Menu")]
[SerializeField] private Button singlePlayerButton;
[SerializeField] private Button multiPlayerButton;

private bool alreadyLoaded = false;


// =========================================================
// START
// =========================================================

private void Start()
{


    // Validar que no se haya cargado antes.
    if (!alreadyLoaded)
    {
        alreadyLoaded = true;
        if (singlePlayerButton != null)
        {
        singlePlayerButton.onClick.AddListener(StartSinglePlayer);
        }
        if (multiPlayerButton != null)
        {
        multiPlayerButton.onClick.AddListener(StartMultiPlayer);
        }
    }
// =========================================================
// INICIAR MODO CONTRARELOJ / SINGLEPLAYER
// =========================================================

void StartSinglePlayer()
{
    SceneManager.LoadScene("JuanScene");
}

// =========================================================
// INICIAR MODO VERSUS / MULTIPLAYER
// =========================================================

void StartMultiPlayer()
    {
    SceneManager.LoadScene("MultiplayerScene");
    }
}
}
