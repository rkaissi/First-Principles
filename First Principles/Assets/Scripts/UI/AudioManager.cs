using System.Collections;
using UnityEngine;

/// <summary>
/// Legacy / optional music cross-fade helper (SceneFader references are commented out).
/// Singleton pattern via static <c>musicplay</c>; extend with your own track list if re-enabling.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private AudioSource[] _audioSources;
    public float fadeSpeed = 0.5f;
    public float fadeInOutMultiplier = 0.0f;
    public bool isPlaying;

    public string playingTrackName = "Nothing";
    public int playingTrackIndex;
    public float playingTrackVolume = 0.000f;

    public string lastTrackName = "Nothing";
    public int lastTrackIndex;
    public float lastTrackVolume = 0.000f;

    public static AudioManager musicplay;
    public bool Always;

    public IEnumerator FadeOutOldMusic_FadeInNewMusic()
    {
        _audioSources[playingTrackIndex].volume = 0.000f;
        _audioSources[playingTrackIndex].Play();
        while (_audioSources[playingTrackIndex].volume < 1f)
        {
            _audioSources[lastTrackIndex].volume -= fadeSpeed * 2;
            _audioSources[playingTrackIndex].volume += fadeSpeed * 2;
            //Debug.Log("Fade: " + lastTrackName + " " + _audioSources[lastTrackIndex].volume.ToString() + " Rise: " + playingTrackName + " " + _audioSources[playingTrackIndex].volume.ToString());
            yield return new WaitForSeconds(0.001f);
            lastTrackVolume = _audioSources[lastTrackIndex].volume;
            playingTrackVolume = _audioSources[playingTrackIndex].volume;
        }
        _audioSources[lastTrackIndex].volume = 0.000f; // Just In Case....
        _audioSources[lastTrackIndex].Stop();

        lastTrackIndex = playingTrackIndex;
        lastTrackName = playingTrackName;
        isPlaying = true;
    }

    public IEnumerator FadeInNewMusic()
    {
        _audioSources[playingTrackIndex].volume = 0.000f;
        _audioSources[playingTrackIndex].Play();
        while (_audioSources[playingTrackIndex].volume < 1f)
        {
            _audioSources[playingTrackIndex].volume += fadeSpeed * 2;
            //Debug.Log("Fading In: " + _audioSources[track_index].volume.ToString());
            yield return new WaitForSeconds(0.001f);
            playingTrackVolume = _audioSources[playingTrackIndex].volume;
        }
        lastTrackIndex = playingTrackIndex;
        lastTrackName = playingTrackName;
        isPlaying = true;
    }

    private void Awake()
    {
        if (musicplay == null)
        {
            DontDestroyOnLoad(transform.gameObject);
            musicplay = this;
        }
        else if (musicplay != this)
        {
            Destroy(transform.gameObject);
        }

        _audioSources = GetComponentsInChildren<AudioSource>();
    }

    public void PlayMusic(string transformName)
    {
        for (int a = 0; a < _audioSources.Length; a++)
        {
            if (_audioSources[a].name == transformName)
            {
                Debug.Log("Found Track Name (" + transformName + ") at Index(" + a.ToString() + ")");
                playingTrackIndex = a;
                playingTrackName = transformName;
                break;
            }
        }
        if (playingTrackIndex == lastTrackIndex)
        {
            Debug.Log("Same Track Selected");
            return;
        }
        else
        {
            if (isPlaying)
            {
                Debug.Log("Fading in new music - Fading out old music");
                StartCoroutine(FadeOutOldMusic_FadeInNewMusic());
            }
            else
            {
                Debug.Log("Fading in new music");
                StartCoroutine(FadeInNewMusic());
            }
        }
    }
}

// LINK: https://forum.unity.com/threads/fade-out-audio-source.335031/