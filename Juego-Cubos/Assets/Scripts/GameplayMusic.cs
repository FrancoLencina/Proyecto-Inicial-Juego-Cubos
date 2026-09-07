using UnityEngine;

public class GameplayMusic : MonoBehaviour
{
    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameplayMusic();
        }
    }
}