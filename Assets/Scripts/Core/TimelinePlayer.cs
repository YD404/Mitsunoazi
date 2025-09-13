// Assets/Scripts/Core/TimelinePlayer.cs
using UnityEngine;
using UnityEngine.Playables;
using System.IO;
using System.Collections.Generic;

// ★SubDisplayManagerを使用するため、namespaceを追記（プロジェクト構成に合わせる）
namespace Mitsunoazi
{
    public class TimelinePlayer : MonoBehaviour
    {
        [Header("Timeline Prefabs")]
        [SerializeField] private List<GameObject> crazyTimelinePrefabs;
        [SerializeField] private List<GameObject> attackerTimelinePrefabs;
        [SerializeField] private List<GameObject> blockerTimelinePrefabs;
        [SerializeField] private List<GameObject> healerTimelinePrefabs;

        [Header("Dependencies")] // ★ヘッダーを追加して整理
        [SerializeField] private SubDisplayManager subDisplayManager; // ★SubDisplayManagerへの参照を追加

        public void Play(string imagePath, StatusManager.Status status)
        {
            List<GameObject> targetList = GetPrefabListByStatus(status);

            if (targetList == null || targetList.Count == 0)
            {
                Debug.LogWarning($"[TimelinePlayer] No timeline prefabs assigned for status: {status}");
                return;
            }

            GameObject prefabToPlay = targetList[Random.Range(0, targetList.Count)];
            GameObject timelineInstance = Instantiate(prefabToPlay, this.transform);

            PlayableDirector director = timelineInstance.GetComponent<PlayableDirector>();
            TimelineInfo timelineInfo = timelineInstance.GetComponent<TimelineInfo>();

            // ★TimelineInfoのプロパティ名が仕様書と異なるため、提供されたコードに合わせる
            if (director == null || timelineInfo == null || timelineInfo.ImageDisplayRenderer == null)
            {
                Debug.LogError($"[TimelinePlayer] The prefab '{prefabToPlay.name}' is not configured correctly.", timelineInstance);
                Destroy(timelineInstance);
                return;
            }

            Sprite sprite = LoadSpriteFromFile(imagePath);
            if (sprite != null)
            {
                timelineInfo.ImageDisplayRenderer.sprite = sprite;
            }
            
            // ★再生終了時の処理を専用メソッドに委譲
            director.stopped += (d) => OnTimelineStopped(timelineInstance, imagePath);

            director.Play();
        }

        // ★再生終了時に呼び出されるメソッドを新設
        private void OnTimelineStopped(GameObject timelineInstance, string imagePath)
        {
            // 1. サブディスプレイに画像を追加するよう通知
            if (subDisplayManager != null)
            {
                subDisplayManager.AddNewImage(imagePath);
            }
            else
            {
                // SubDisplayManagerが設定されていない場合でも動作は継続させる
                Debug.LogWarning("[TimelinePlayer] SubDisplayManager is not assigned. Skipping sub-display update.");
            }

            // 2. タイムラインインスタンスを破棄
            if (timelineInstance != null)
            {
                // stoppedイベントは複数回呼ばれる可能性を考慮し、リスナーを明示的に解除することが望ましい
                PlayableDirector director = timelineInstance.GetComponent<PlayableDirector>();
                if(director != null)
                {
                    director.stopped -= (d) => OnTimelineStopped(timelineInstance, imagePath);
                }
                Destroy(timelineInstance);
            }
        }

        private List<GameObject> GetPrefabListByStatus(StatusManager.Status status)
        {
            switch (status)
            {
                case StatusManager.Status.Crazy:    return crazyTimelinePrefabs;
                case StatusManager.Status.Attacker: return attackerTimelinePrefabs;
                case StatusManager.Status.Blocker:  return blockerTimelinePrefabs;
                case StatusManager.Status.Healer:   return healerTimelinePrefabs;
                default:                            return null;
            }
        }

        private Sprite LoadSpriteFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[TimelinePlayer] Image file not found at: {path}");
                return null;
            }
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
