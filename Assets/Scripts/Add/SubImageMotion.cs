using UnityEngine;

[RequireComponent(typeof(SubImagePosition))]
public class SubImageMotion : MonoBehaviour
{
    [Header("モーション設定")]
    [SerializeField, Tooltip("動きの速さ")]
    private float motionSpeed = 0.5f;

    [Header("動きの範囲")]
    [SerializeField, Tooltip("動きをCanvasの表示範囲に制限する")]
    private bool limitToCanvasBounds = true;

    [SerializeField, Tooltip("limitToCanvasBoundsがfalseの場合の動きの大きさ（半径）")]
    private float motionMagnitude = 10f;
    
    [Header("開始アニメーション")]
    [SerializeField, Tooltip("モーション開始時のフェードイン時間")]
    private float motionFadeInDuration = 1.0f;

    private RectTransform rectTransform;
    private Vector2 anchorPosition;
    private float randomSeedX;
    private float randomSeedY;
    private SubImagePosition subImagePosition;
    private float startTime;
    
    private Canvas rootCanvas;
    // x:右, y:上, z:左, w:下 への最大移動距離
    private Vector4 moveLimits;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        randomSeedX = Random.Range(0f, 100f);
        randomSeedY = Random.Range(0f, 100f);
        subImagePosition = GetComponent<SubImagePosition>();
        rootCanvas = GetComponentInParent<Canvas>();
        
        this.enabled = false;
    }

    public void ActivateMotion()
    {
        this.anchorPosition = subImagePosition.AnchorPosition;
        this.startTime = Time.time;
        
        // Canvas範囲制限が有効な場合、移動範囲を計算する
        if (limitToCanvasBounds && rootCanvas != null)
        {
            CalculateMoveLimits();
        }
        
        this.enabled = true;
    }

    /// <summary>
    /// 基準位置からCanvasの各境界までの移動可能距離を計算する
    /// </summary>
    private void CalculateMoveLimits()
    {
        Rect canvasRect = rootCanvas.GetComponent<RectTransform>().rect;
        Rect selfRect = rectTransform.rect;
        Vector2 selfPivot = rectTransform.pivot;

        // 基準位置から見て、オブジェクトがCanvas内に収まるための各境界までの距離
        float limitRight = (canvasRect.xMax - (selfRect.width * (1 - selfPivot.x))) - anchorPosition.x;
        float limitUp = (canvasRect.yMax - (selfRect.height * (1 - selfPivot.y))) - anchorPosition.y;
        float limitLeft = anchorPosition.x - (canvasRect.xMin + (selfRect.width * selfPivot.x));
        float limitDown = anchorPosition.y - (canvasRect.yMin + (selfRect.height * selfPivot.y));
        
        // 負の値は0にクランプし、Vector4に格納
        moveLimits = new Vector4(
            Mathf.Max(0, limitRight), 
            Mathf.Max(0, limitUp), 
            Mathf.Max(0, limitLeft), 
            Mathf.Max(0, limitDown)
        );
    }

    void Update()
    {
        // -1.0f から 1.0f の範囲のノイズを計算
        float noiseX = Mathf.PerlinNoise(Time.time * motionSpeed, randomSeedX);
        float noiseY = Mathf.PerlinNoise(Time.time * motionSpeed, randomSeedY);
        float offsetX = (noiseX - 0.5f) * 2f;
        float offsetY = (noiseY - 0.5f) * 2f;
        
        Vector2 finalOffset;

        if (limitToCanvasBounds)
        {
            // Canvasの境界までの距離に応じてオフセットをスケーリング
            float finalOffsetX = (offsetX > 0) ? offsetX * moveLimits.x : offsetX * moveLimits.z;
            float finalOffsetY = (offsetY > 0) ? offsetY * moveLimits.y : offsetY * moveLimits.w;
            finalOffset = new Vector2(finalOffsetX, finalOffsetY);
        }
        else
        {
            // motionMagnitudeを半径としてオフセットをスケーリング
            finalOffset = new Vector2(offsetX, offsetY) * motionMagnitude;
        }

        // フェードイン処理
        float elapsedTime = Time.time - startTime;
        if (motionFadeInDuration > 0f && elapsedTime < motionFadeInDuration)
        {
            finalOffset *= (elapsedTime / motionFadeInDuration);
        }
        
        rectTransform.anchoredPosition = anchorPosition + finalOffset;
    }
}
