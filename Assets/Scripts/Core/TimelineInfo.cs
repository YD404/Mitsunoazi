// Assets/Scripts/Core/TimelineInfo.cs
using UnityEngine;

// タイムラインプレハブにアタッチするコンポーネント
public class TimelineInfo : MonoBehaviour
{
    // 画像を表示するSpriteRendererをインスペクタから設定する
    [Tooltip("キャプチャ画像を表示するSpriteRenderer")]
    public SpriteRenderer ImageDisplayRenderer;
}
