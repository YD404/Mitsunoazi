// BgmLooper.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 新Input System用
#endif

[RequireComponent(typeof(AudioSource))]
public class BgmLooper : MonoBehaviour
{
    [Header("BGM設定")]
    [Tooltip("再生するBGMクリップ")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    [Tooltip("起動時の音量")]
    public float startVolume = 0.5f;

    [Header("ホイール調整")]
    [Tooltip("ホイール1ノッチあたりの音量感度（絶対値）")]
    public float wheelSensitivity = 0.01f;

    [Tooltip("音量の最小/最大")]
    public Vector2 volumeRange = new Vector2(0f, 1f);

    private AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.loop = true;
        _src.playOnAwake = false;
        _src.clip = bgmClip;
        _src.volume = Mathf.Clamp(startVolume, volumeRange.x, volumeRange.y);
        _src.spatialBlend = 0f; // 2Dサウンド
    }

    void OnEnable()
    {
        if (_src.clip != null && !_src.isPlaying)
            _src.Play();
    }

    void Update()
    {
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float raw = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(raw) > 0f)
                scroll = Mathf.Sign(raw);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (scroll == 0f)
        {
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }
#endif

        if (Mathf.Abs(scroll) > 0f)
        {
            float step = (Mathf.Abs(scroll) > 0.5f) ? Mathf.Sign(scroll) * wheelSensitivity
                                                    : scroll * (wheelSensitivity * 10f);
            _src.volume = Mathf.Clamp(_src.volume + step, volumeRange.x, volumeRange.y);

            // ★ デバッグログ追加 ★
            Debug.Log($"[BgmLooper] Current Volume: {_src.volume:F2}");
        }
    }
}
