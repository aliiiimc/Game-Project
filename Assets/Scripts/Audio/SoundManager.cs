using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    [Header("Music")]
    public AudioClip themeClip;
    [Range(0f,1f)] public float musicVolume = 0.6f;

    [Header("SFX")]
    public AudioSource sfxSource;
    [Range(0f,1f)] public float sfxVolume = 0.9f;

    private AudioSource musicSource;
    private bool isMuted = false;
    private const string MutePrefKey = "SoundMuted";

    public bool IsMuted
    {
        get { return isMuted; }
        set
        {
            isMuted = value;
            PlayerPrefs.SetInt(MutePrefKey, isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMuteState();
        }
    }

    public static SoundManager GetOrCreate()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<SoundManager>();
        if (instance != null) return instance;

        GameObject go = new GameObject("SoundManager");
        instance = go.AddComponent<SoundManager>();
        DontDestroyOnLoad(go);
        instance.EnsureSources();
        return instance;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureSources();
        isMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;
        ApplyMuteState();
    }

    void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
        }

        if (sfxSource == null)
        {
            // create a dedicated SFX source if none assigned
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    public void ApplyMuteState()
    {
        EnsureSources();
        if (musicSource != null) musicSource.mute = isMuted;
        if (sfxSource != null) sfxSource.mute = isMuted;
    }

    public void PlayTheme()
    {
        if (themeClip == null)
        {
            return;
        }

        EnsureSources();
        musicSource.clip = themeClip;
        musicSource.volume = musicVolume;
        musicSource.mute = isMuted;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopTheme()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureSources();
        sfxSource.mute = isMuted;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }
}
