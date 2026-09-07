using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Juego cerrado");
    }
}