using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KeyNumberSound : MonoBehaviour
{
    [Header("全キーで鳴らす共通の AudioClip（mp3 可）")]
    public AudioClip clip;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) ||
            Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Alpha2) ||
            Input.GetKeyDown(KeyCode.Alpha3))
        {
            Play();
        }
    }

    void Play()
    {
        if (clip == null) return;
        src.PlayOneShot(clip);
    }
}
