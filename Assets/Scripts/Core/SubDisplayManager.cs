using System.Collections;
using System.Collections.Generic; // Listを使用するために追加
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Mitsunoazi
{
    public class SubDisplayManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject imagePrefab; // InspectorからSubImagePrefabをアサイン

        [SerializeField]
        private Transform imageContainer; // InspectorからImageContainerをアサイン

        [SerializeField]
        private int maxImageCount = 50; // 画像の最大数をInspectorから設定可能にする

        private string confirmedImagePath;

        // 生成したイメージオブジェクトを管理するためのリスト
        private List<GameObject> spawnedImages = new List<GameObject>();

        private void Start()
        {
            confirmedImagePath = Path.Combine(Application.streamingAssetsPath, "ImageConfirmed");
            if (!Directory.Exists(confirmedImagePath))
            {
                Directory.CreateDirectory(confirmedImagePath);
            }

            LoadAndDisplayAllImages();
        }

        // 起動時に既存の画像をすべて表示する
        private void LoadAndDisplayAllImages()
        {
            string[] files = Directory.GetFiles(confirmedImagePath, "*.png");
            foreach (string file in files)
            {
                StartCoroutine(LoadAndCreateImage(file));
            }
        }

        // 新しい画像を追加表示するためのPublicメソッド
        public void AddNewImage(string imagePath)
        {
            StartCoroutine(LoadAndCreateImage(imagePath));
        }

        // 画像ファイルを読み込み、UIとしてインスタンス化するコルーチン
        private IEnumerator LoadAndCreateImage(string path)
        {
            byte[] pngBytes = File.ReadAllBytes(path);

            // テクスチャとしてロード
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(pngBytes);

            yield return null; // 1フレーム待機

            // UIプレハブをインスタンス化
            GameObject imageObject = Instantiate(imagePrefab, imageContainer);
            Image imageComponent = imageObject.GetComponent<Image>();

            // テクスチャからスプライトを作成してアサイン
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            imageComponent.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

            // アスペクト比を維持
            imageComponent.preserveAspect = true;

            // --- ここから追加した処理 ---

            // 管理リストに新しいオブジェクトを追加
            spawnedImages.Add(imageObject);

            // オブジェクトの数が上限を超えていたら、一番古いものを削除
            if (spawnedImages.Count > maxImageCount)
            {
                // リストの先頭にあるゲームオブジェクト（一番古いもの）を取得
                GameObject oldestImage = spawnedImages[0];

                // リストから削除
                spawnedImages.RemoveAt(0);

                // シーンからゲームオブジェクトを破棄
                Destroy(oldestImage);
            }
            // --- ここまで ---
        }
    }
}