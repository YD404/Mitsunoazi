using UnityEngine;

// この属性により、エディタ上で再生ボタンを押さなくてもスクリプトが動作します
[ExecuteInEditMode]
public class QuadrantPlacer : MonoBehaviour
{
    // どの隅に配置するかを選択するためのリスト
    public enum ScreenQuadrant
    {
        TopRight,   // 右上
        BottomRight, // 右下
        BottomLeft,  // 左下
        TopLeft     // 左上
    }

    [Header("配置場所")]
    [Tooltip("このオブジェクトを画面のどの隅に配置するかを選択してください")]
    public ScreenQuadrant quadrant;

    private Camera mainCamera;

    // インスペクターの値が変更された時やオブジェクトが動いた時に呼ばれます
    void Update()
    {
        // アプリケーションが再生中でない場合のみ実行（エディタ上でのみ動作させたい場合）
        // もしゲーム実行中にも位置を固定したい場合は、このif文を削除してください。
        if (!Application.isPlaying)
        {
            PositionObject();
        }
    }

    // オブジェクトが有効になった時に一度だけ呼ばれます
    void OnEnable()
    {
        PositionObject();
    }

    void PositionObject()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // カメラが見つからない場合は何もしない
            return;
        }

        Vector2 viewportPosition = Vector2.zero;

        // 選択された場所に応じて、目標となるビューポート座標を設定
        switch (quadrant)
        {
            case ScreenQuadrant.TopRight:
                viewportPosition = new Vector2(0.75f, 0.75f);
                break;
            case ScreenQuadrant.BottomRight:
                viewportPosition = new Vector2(0.75f, 0.25f);
                break;
            case ScreenQuadrant.BottomLeft:
                viewportPosition = new Vector2(0.25f, 0.25f);
                break;
            case ScreenQuadrant.TopLeft:
                viewportPosition = new Vector2(0.25f, 0.75f);
                break;
        }

        // プレハブ自体のZ座標を維持しつつ、XY座標を計算
        float zDistance = transform.position.z - mainCamera.transform.position.z;
        Vector3 targetWorldPosition = mainCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, zDistance));

        // 計算した座標をオブジェクトに適用
        transform.position = targetWorldPosition;
    }
}