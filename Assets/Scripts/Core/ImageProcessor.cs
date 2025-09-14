// Assets/Scripts/Core/ImageProcessor.cs
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

namespace Mitsunoazi
{
    /// <summary>
    /// 画像の処理（透過、保存、移動）を専門に担う静的クラス。
    /// </summary>
    public static class ImageProcessor
    {
        // ... (メソッドの実装は変更なし) ...
        
        /// <summary>
        /// 画像を透過処理し、指定パスに非同期で保存する。
        /// </summary>
        public static async Task ProcessAndSaveImageAsync(Texture2D image, string outputPath)
        {
            if (!image.isReadable)
            {
                Debug.LogError("Texture is not readable. Please enable 'Read/Write' in import settings.");
                return;
            }

            Color32[] pixels = image.GetPixels32();
            var brightnessList = new System.Collections.Generic.List<float>(pixels.Length);
            for (int i = 0; i < pixels.Length; i++)
            {
                float brightness = (pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f) / 255f;
                brightnessList.Add(brightness);
            }

            brightnessList.Sort((a, b) => b.CompareTo(a));
            int thresholdIndex = (int)(pixels.Length * 0.1f);
            if (thresholdIndex >= brightnessList.Count)
            {
                thresholdIndex = brightnessList.Count - 1;
            }
            float threshold = brightnessList[thresholdIndex];

            for (int i = 0; i < pixels.Length; i++)
            {
                float brightness = (pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f) / 255f;
                if (brightness >= threshold)
                {
                    pixels[i].a = 0;
                }
            }

            Texture2D processedTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            processedTexture.SetPixels32(pixels);
            processedTexture.Apply();

            byte[] bytes = processedTexture.EncodeToPNG();
            Object.Destroy(processedTexture);

            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        /// <summary>
        /// 生の画像をPNGとして非同期で保存する。
        /// </summary>
        public static async Task SaveRawImageAsync(Texture2D image, string outputPath)
        {
            byte[] bytes = image.EncodeToPNG();
            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        /// <summary>
        /// ステータス確定後、ファイルをStagedからConfirmedへ移動する。
        /// </summary>
        public static void MoveAndRenameConfirmedFile(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                string directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                Debug.LogError($"Source file not found for moving: {sourcePath}");
            }
        }
    }
}
