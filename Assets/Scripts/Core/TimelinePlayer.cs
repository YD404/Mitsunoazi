using UnityEngine;
using UnityEngine.Playables;
using System.IO;
using System.Collections.Generic;

namespace Mitsunoazi
{
    public class TimelinePlayer : MonoBehaviour
    {
        [Header("Timeline Prefabs")]
        [SerializeField] private List<GameObject> crazyTimelinePrefabs;
        [SerializeField] private List<GameObject> attackerTimelinePrefabs;
        [SerializeField] private List<GameObject> blockerTimelinePrefabs;
        [SerializeField] private List<GameObject> healerTimelinePrefabs;

        [Header("Dependencies")]
        [SerializeField] private SubDisplayManager subDisplayManager;

        // クリックタイプを表すenum
        public enum ClickType { Left, Right }

        public void Play(string imagePath, StatusManager.Status status, ClickType clickType)
        {
            Debug.Log($"[TimelinePlayer] タイムライン再生開始 - ステータス: {status}, 画像: {imagePath}, クリック: {clickType}");

            Director.IsTimelinePlaying = true;
            Debug.Log($"[TimelinePlayer] タイムライン再生フラグをONに設定");

            List<GameObject> targetList = GetPrefabListByStatus(status);

            if (targetList == null || targetList.Count == 0)
            {
                Debug.LogWarning($"[TimelinePlayer] ステータス {status} に対応するタイムラインプレハブがありません");
                Director.IsTimelinePlaying = false;
                Debug.Log($"[TimelinePlayer] 再生失敗のためフラグをOFFに設定");
                return;
            }

            // クリックタイプに基づいてプレハブを選択
            GameObject prefabToPlay = GetPrefabByClickType(targetList, clickType);
            
            if (prefabToPlay == null)
            {
                Debug.LogError($"[TimelinePlayer] プレハブの選択に失敗しました");
                Director.IsTimelinePlaying = false;
                Debug.Log($"[TimelinePlayer] 選択失敗のためフラグをOFFに設定");
                return;
            }

            Debug.Log($"[TimelinePlayer] プレハブ選択: {prefabToPlay.name} (クリックタイプ: {clickType})");
    
            GameObject timelineInstance = Instantiate(prefabToPlay, this.transform);
            Debug.Log($"[TimelinePlayer] タイムラインインスタンス生成");

            PlayableDirector director = timelineInstance.GetComponent<PlayableDirector>();
            TimelineInfo timelineInfo = timelineInstance.GetComponent<TimelineInfo>();

            if (director == null || timelineInfo == null || timelineInfo.ImageDisplayRenderer == null)
            {
                Debug.LogError($"[TimelinePlayer] プレハブ '{prefabToPlay.name}' の設定が不正です");
                Destroy(timelineInstance);
                Director.IsTimelinePlaying = false;
                Debug.Log($"[TimelinePlayer] 設定エラーのためフラグをOFFに設定");
                return;
            }

            Sprite sprite = LoadSpriteFromFile(imagePath);
            if (sprite != null)
            {
                timelineInfo.ImageDisplayRenderer.sprite = sprite;
                Debug.Log($"[TimelinePlayer] 画像をスプライトに設定: {imagePath}");
            }
            else
            {
                Debug.LogWarning($"[TimelinePlayer] 画像の読み込みに失敗: {imagePath}");
            }
    
            director.stopped += (d) => OnTimelineStopped(timelineInstance, imagePath);
            director.Play();
            Debug.Log($"[TimelinePlayer] プレイアブルディレクター再生開始");
        }

        private GameObject GetPrefabByClickType(List<GameObject> prefabList, ClickType clickType)
        {
            if (prefabList == null || prefabList.Count == 0)
                return null;

            // クリックタイプに基づいてインデックスを決定
            int index = (clickType == ClickType.Left) ? 0 : 1;
            
            // インデックスがリスト範囲内かチェック
            if (index >= prefabList.Count)
            {
                Debug.LogWarning($"[TimelinePlayer] インデックス {index} はリスト範囲外です。リストサイズ: {prefabList.Count}. 代わりに最初の要素を使用します。");
                index = 0;
            }
            
            return prefabList[index];
        }

        private void OnTimelineStopped(GameObject timelineInstance, string imagePath)
        {
            Debug.Log($"[TimelinePlayer] タイムライン再生終了コールバック");

            if (subDisplayManager != null)
            {
                subDisplayManager.AddNewImage(imagePath);
                Debug.Log($"[TimelinePlayer] サブディスプレイに画像追加通知: {imagePath}");
            }
            else
            {
                Debug.LogWarning("[TimelinePlayer] SubDisplayManagerが未設定です");
            }

            if (timelineInstance != null)
            {
                PlayableDirector director = timelineInstance.GetComponent<PlayableDirector>();
                if(director != null)
                {
                    director.stopped -= (d) => OnTimelineStopped(timelineInstance, imagePath);
                    Debug.Log($"[TimelinePlayer] イベントリスナー解除");
                }
                Destroy(timelineInstance);
                Debug.Log($"[TimelinePlayer] タイムラインインスタンス破棄");
            }

            Director.IsTimelinePlaying = false;
            Debug.Log($"[TimelinePlayer] タイムライン再生フラグをOFFに設定");
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
