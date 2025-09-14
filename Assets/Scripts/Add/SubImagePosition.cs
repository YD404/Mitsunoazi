using System.Collections;
using UnityEngine;

// 他のデフォルトスクリプトより先に実行するための属性
[DefaultExecutionOrder(-100)]
public class SubImagePosition : MonoBehaviour
{
    private RectTransform rectTransform;

    /// <summary>
    /// レイアウト確定後の最終的な基準座標
    /// </summary>
    public Vector2 AnchorPosition { get; private set; }

    /// <summary>
    /// 座標の初期化が完了したかどうか
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    IEnumerator Start()
    {
        // UIの自動レイアウトが完全に確定するまで1フレーム待機
        yield return new WaitForEndOfFrame();

        // 確定した座標を保存
        AnchorPosition = rectTransform.anchoredPosition;
        IsInitialized = true;
        
        // このコンポーネントの役目は終わったので無効化
        this.enabled = false;
    }
}
