using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(SubImageMotion))]
[RequireComponent(typeof(SubImagePosition))] // 依存関係として追加
public class SubImageStart : MonoBehaviour
{
    [Header("アニメーション設定")]
    [SerializeField, Tooltip("アニメーション全体の時間")]
    private float animationDuration = 0.7f;
    [SerializeField, Tooltip("開始時のスケール")]
    private float startScale = 0.5f;
    [SerializeField, Tooltip("どこから登場するかのオフセット")]
    private Vector2 startPositionOffset = new Vector2(0, -50f);

    private CanvasGroup canvasGroup;
    private SubImageMotion subImageMotion;
    private RectTransform rectTransform;
    private SubImagePosition subImagePosition; // 参照を追加

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        subImageMotion = GetComponent<SubImageMotion>();
        rectTransform = GetComponent<RectTransform>();
        subImagePosition = GetComponent<SubImagePosition>(); // 参照を取得
    }

    void Start()
    {
        StartCoroutine(StartAnimation());
    }

    private IEnumerator StartAnimation()
    {
        // SubImagePositionによる位置の初期化が完了するまで待機
        while (!subImagePosition.IsInitialized)
        {
            yield return null;
        }

        // SubImagePositionから確定済みの最終位置を取得
        Vector2 finalPosition = subImagePosition.AnchorPosition;

        // アニメーションの初期状態を設定
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * startScale;
        rectTransform.anchoredPosition = finalPosition + startPositionOffset;

        // アニメーションループ
        float time = 0;
        while (time < animationDuration)
        {
            float progress = time / animationDuration;
            float easedProgress = 1 - Mathf.Pow(1 - progress, 3);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, easedProgress);
            transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one, easedProgress);
            rectTransform.anchoredPosition = Vector2.Lerp(finalPosition + startPositionOffset, finalPosition, easedProgress);
            
            time += Time.deltaTime;
            yield return null;
        }

        // アニメーションの値を最終値に確定
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        rectTransform.anchoredPosition = finalPosition;

        // アニメーション完了後、SubImageMotionを有効化
        if (subImageMotion != null)
        {
            subImageMotion.ActivateMotion();
        }
        
        this.enabled = false;
    }
}
