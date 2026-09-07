using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("MainMenuMusicController iniciado.");

        if (SoundManager.Instance == null)
        {
            Debug.LogError(
                "SoundManager es NULL en el menú."
            );

            return;
        }

        Debug.Log("Reproduciendo música del menú.");

        SoundManager.Instance.PlayMenuMusic();
    }
}