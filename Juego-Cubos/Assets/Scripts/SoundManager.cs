using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;


    // =====================================================
    // AUDIO SOURCES
    // =====================================================

    [Header("Audio Sources")]

    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioSource musicSource;


    // =====================================================
    // SOUND EFFECTS
    // =====================================================

    [Header("Movement")]

    public AudioClip footstepSound;
    public AudioClip jumpSound;
    public AudioClip landSound;


    [Header("UI")]

    public AudioClip buttonClickSound;
    public AudioClip pauseOpenSound;
    public AudioClip pauseCloseSound;


    [Header("FruitBlocks")]

    public AudioClip grabBlockSound;
    public AudioClip dropBlockSound;
    public AudioClip correctPlacementSound;


    [Header("Game")]

    public AudioClip winSound;
    public AudioClip loseSound;


    // =====================================================
    // MUSIC
    // =====================================================

    [Header("Background Music")]

    public AudioClip menuMusic;

    public AudioClip gameplayMusic;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(
                gameObject
            );
        }
        else
        {
            Destroy(
                gameObject
            );

            return;
        }


        // -------------------------------------------------
        // SFX SOURCE
        // -------------------------------------------------

        if (sfxSource == null)
        {
            sfxSource =
                gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake =
            false;

        sfxSource.loop =
            false;

        sfxSource.spatialBlend =
            0f;


        // -------------------------------------------------
        // MUSIC SOURCE
        // -------------------------------------------------

        if (musicSource == null)
        {
            musicSource =
                gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake =
            false;

        musicSource.loop =
            true;

        musicSource.spatialBlend =
            0f;
    }


    // =====================================================
    // GENERIC SOUND
    // =====================================================

    public void PlaySound(
        AudioClip clip
    )
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(
                clip
            );
        }
    }


    // =====================================================
    // SOUND EFFECT METHODS
    // =====================================================

    public void PlayFootstep()
    {
        PlaySound(footstepSound);
    }

    public void PlayJump()
    {
        PlaySound(jumpSound);
    }

    public void PlayLand()
    {
        PlaySound(landSound);
    }

    public void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    public void PlayGrabBlock()
    {
        PlaySound(grabBlockSound);
    }

    public void PlayDropBlock()
    {
        PlaySound(dropBlockSound);
    }

    public void PlayCorrectPlacement()
    {
        PlaySound(correctPlacementSound);
    }

    public void PlayPauseOpen()
    {
        PlaySound(pauseOpenSound);
    }

    public void PlayPauseClose()
    {
        PlaySound(pauseCloseSound);
    }

    public void PlayWin()
    {
        PlaySound(winSound);
    }

    public void PlayLose()
    {
        PlaySound(loseSound);
    }


    // =====================================================
    // MUSIC
    // =====================================================

    public void PlayMenuMusic()
    {
        Debug.Log(
            "[SoundManager] PlayMenuMusic llamado. Clip: " +
            (menuMusic != null
                ? menuMusic.name
                : "NULL")
        );

        PlayMusic(
            menuMusic
        );
    }


    public void PlayGameplayMusic()
    {
        PlayMusic(
            gameplayMusic
        );
    }


    private void PlayMusic(
        AudioClip musicClip
    )
    {
        if (musicClip == null)
        {
            Debug.LogError(
                "[SoundManager] El AudioClip recibido es NULL."
            );

            return;
        }


        if (musicSource == null)
        {
            Debug.LogError(
                "[SoundManager] musicSource es NULL."
            );

            return;
        }


        Debug.Log(
            "[SoundManager] Reproduciendo: " +
            musicClip.name
        );


        if (
            musicSource.clip == musicClip &&
            musicSource.isPlaying
        )
        {
            Debug.Log(
                "[SoundManager] La música ya estaba reproduciéndose."
            );

            return;
        }


        musicSource.clip =
            musicClip;

        musicSource.Play();
    }


    public void StopMusic()
    {
        musicSource.Stop();
    }
}