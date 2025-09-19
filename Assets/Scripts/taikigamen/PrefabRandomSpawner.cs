using UnityEngine;
using System.Collections; // Coroutineを使うために必要

public class PrefabRandomSpawner : MonoBehaviour
{
    // Inspectorから設定する項目
    [Header("生成するプレハブ")]
    [Tooltip("ここに生成したいプレハブを複数設定できます")]
    public GameObject[] prefabsToSpawn;

    [Header("生成間隔（秒）")]
    [Tooltip("プレハブが生成される最短時間")]
    public float minSpawnInterval = 1.0f;

    [Tooltip("プレハブが生成される最長時間")]
    public float maxSpawnInterval = 5.0f;

    [Header("生成エリア設定")]
    [Tooltip("画面の端からどれくらい内側に生成するか")]
    public float padding = 1.0f;

    // 内部で使う変数
    private Camera mainCamera;
    private float screenLeft, screenRight, screenBottom, screenTop;

    void Start()
    {
        // メインカメラの情報を取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("エラー: シーンにメインカメラが見つかりません。");
            return;
        }

        // 生成するプレハブが設定されているかチェック
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
        {
            Debug.LogError("エラー: 生成するプレハブが設定されていません。");
            return;
        }

        // カメラの視野に基づいて生成範囲を計算する
        CalculateSpawnArea();

        // 生成処理のコルーチンを開始する
        StartCoroutine(SpawnRoutine());
    }

    void CalculateSpawnArea()
    {
        // カメラの左下(0,0)と右上(1,1)のワールド座標を取得
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));

        // パディングを考慮して範囲を設定
        screenLeft = bottomLeft.x + padding;
        screenRight = topRight.x - padding;
        screenBottom = bottomLeft.y + padding;
        screenTop = topRight.y - padding;
    }

    // プレハブを生成し続けるコルーチン
    private IEnumerator SpawnRoutine()
    {
        // このループはゲームが実行されている間、ずっと続く
        while (true)
        {
            // 1. 次の生成までの待機時間をランダムに決める
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // 2. 生成するプレハブをランダムに選ぶ
            GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];

            // 3. 生成する場所をランダムに決める
            float spawnX = Random.Range(screenLeft, screenRight);
            float spawnY = Random.Range(screenBottom, screenTop);
            Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0); // Z座標は0に設定

            // 4. プレハブを生成する
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }
}