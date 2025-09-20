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
    // public float pattern1_Duration = 15f; // Durationはもう不要です
    public GameObject pattern2_Prefab;
    // public float pattern2_Duration = 20f; // Durationはもう不要です
}

public class Director : MonoBehaviour // もしSequentialDirector.csならそのままでOK
{
    [Header("全体設定")]
    public float minSpawnInterval = 4.0f;
    public float maxSpawnInterval = 8.0f;

    [Header("パターン2の出現頻度")]
    [Min(1)]
    public int pattern2Frequency = 3;

    [Header("各エリアのプレハブ設定（再生したい順番に並べる）")]
    public AreaSpawnConfig[] areaConfigs;

    // ---- 内部管理用 ----
    // ★★★ 変更点：bool[] isAreaBusy の代わりに、GameObjectの配列で管理 ★★★
    private GameObject[] activeClones;
    private int nextAreaIndex = 0;
    private int spawnCounter = 0;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        // ★★★ 変更点：activeClones配列を初期化 ★★★
        activeClones = new GameObject[areaConfigs.Length];
        StartCoroutine(SpawnSequencer());
    }

    private IEnumerator SpawnSequencer()
    {
        while (true)
        {
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(interval);

            // ★★★ 変更点：タイマーではなく、クローンの「存在」を直接確認 ★★★
            // activeClones[nextAreaIndex]に何か入っている（nullではない）間は、待ち続ける
            while (activeClones[nextAreaIndex] != null)
            {
                yield return new WaitForSeconds(1.0f);
            }

            spawnCounter++;
            GameObject prefabToSpawn;

            if (spawnCounter >= pattern2Frequency)
            {
                prefabToSpawn = areaConfigs[nextAreaIndex].pattern2_Prefab;
                spawnCounter = 0;
            }
            else
            {
                prefabToSpawn = areaConfigs[nextAreaIndex].pattern1_Prefab;
            }

            if (prefabToSpawn != null)
            {
                Vector3 position = QuadrantPositioner.GetPosition(areaConfigs[nextAreaIndex].quadrant, mainCamera);

                // ★★★ 変更点：生成したクローンを、activeClones配列に記録する ★★★
                activeClones[nextAreaIndex] = Instantiate(prefabToSpawn, position, Quaternion.identity);
            }

            nextAreaIndex = (nextAreaIndex + 1) % areaConfigs.Length;
        }
    }

    // ★★★ 変更点：ReleaseAreaAfterDelayメソッドは完全に不要になったため削除 ★★★
}