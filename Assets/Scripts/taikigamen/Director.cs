using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AreaSpawnConfig
{
    [Header("エリア設定")]
    public string areaName;
    public QuadrantPositioner.ScreenQuadrant quadrant;
    [Header("プレハブ設定")]
    public GameObject pattern1_Prefab;
    public GameObject pattern2_Prefab;
}

public class Director : MonoBehaviour
{
    [Header("全体設定")]
    public float minSpawnInterval = 4.0f;
    public float maxSpawnInterval = 8.0f;

    [Header("パターン2の出現頻度")]
    [Min(1)]
    public int pattern2Frequency = 3;

    [Header("各エリアのプレハブ設定（再生したい順番に並べる）")]
    public AreaSpawnConfig[] areaConfigs;

    // タイムライン再生中の状態を管理する静的フラグ
    public static bool IsTimelinePlaying { get; set; } = false;

    private GameObject[] activeClones;
    private int nextAreaIndex = 0;
    private int spawnCounter = 0;
    private Camera mainCamera;

    // --- ここから追加 ---
    private bool isPausedByInput = false;
    private Coroutine inputPauseCoroutine;
    // --- ここまで追加 ---

    void Start()
    {
        mainCamera = Camera.main;
        activeClones = new GameObject[areaConfigs.Length];
        StartCoroutine(SpawnSequencer());
    }

    // --- ここから変更 ---
    void Update()
    {
        // 任意のキーが押された場合
        if (Input.anyKeyDown)
        {
            // 既に実行中のタイマーがあれば停止してリセットする
            if (inputPauseCoroutine != null)
            {
                StopCoroutine(inputPauseCoroutine);
            }
            // 5秒間のスポーン停止コルーチンを開始する
            inputPauseCoroutine = StartCoroutine(InputPauseRoutine(5.0f));
        }
    }

    private IEnumerator InputPauseRoutine(float duration)
    {
        if (!isPausedByInput)
        {
            Debug.Log($"[Director] キー入力により、{duration}秒間新規スポーンを一時停止します。");
        }
        isPausedByInput = true;

        yield return new WaitForSeconds(duration);

        isPausedByInput = false;
        inputPauseCoroutine = null; // コルーチン終了
        Debug.Log("[Director] キー入力による一時停止を解除。スポーンを再開します。");
    }
    // --- ここまで変更 ---

    private IEnumerator SpawnSequencer()
    {
        Debug.Log("[Director] スポーンシーケンス開始");

        while (true)
        {
            // --- ここの条件を変更 ---
            // タイムライン再生中、またはキー入力による一時停止中は待機
            if (IsTimelinePlaying || isPausedByInput)
            {
                yield return new WaitUntil(() => !IsTimelinePlaying && !isPausedByInput);
            }

            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            Debug.Log($"[Director] 次のスポーンまで {interval:F1}秒待機");
            yield return new WaitForSeconds(interval);

            while (activeClones[nextAreaIndex] != null)
            {
                Debug.Log($"[Director] エリア {nextAreaIndex} がビジー状態のため待機");
                yield return new WaitForSeconds(1.0f);
            }

            spawnCounter++;
            GameObject prefabToSpawn;

            if (spawnCounter >= pattern2Frequency)
            {
                prefabToSpawn = areaConfigs[nextAreaIndex].pattern2_Prefab;
                spawnCounter = 0;
                Debug.Log($"[Director] パターン2をスポーン (カウンターリセット)");
            }
            else
            {
                prefabToSpawn = areaConfigs[nextAreaIndex].pattern1_Prefab;
                Debug.Log($"[Director] パターン1をスポーン (カウンター: {spawnCounter}/{pattern2Frequency})");
            }

            if (prefabToSpawn != null)
            {
                Vector3 position = QuadrantPositioner.GetPosition(areaConfigs[nextAreaIndex].quadrant, mainCamera);
                activeClones[nextAreaIndex] = Instantiate(prefabToSpawn, position, Quaternion.identity);
                Debug.Log($"[Director] エリア {nextAreaIndex} に {prefabToSpawn.name} をスポーン");
            }
            else
            {
                Debug.LogWarning($"[Director] エリア {nextAreaIndex} のプレハブがnullです");
            }

            nextAreaIndex = (nextAreaIndex + 1) % areaConfigs.Length;
            Debug.Log($"[Director] 次のエリアインデックス: {nextAreaIndex}");
        }
    }
}