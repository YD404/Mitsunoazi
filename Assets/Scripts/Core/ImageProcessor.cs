// Assets/Scripts/Core/ImageProcessor.cs
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mitsunoazi
{
    /// <summary>
    /// 画像の処理（透過、保存、移動）を専門に担う静的クラス。
    /// </summary>
    public static class ImageProcessor
    {
        /// <summary>
        /// 画像を透過処理し、指定パスに非同期で保存する。
        /// 上位10%の輝度を持つピクセルを透明化する。
        /// </summary>
        /// <param name="image">処理対象のTexture2D</param>
        /// <param name="outputPath">保存先のフルパス</param>
        public static async Task ProcessAndSaveImageAsync(Texture2D image, string outputPath)
        {
            if (!image.isReadable)
            {
                Debug.LogError("Texture is not readable. Please enable 'Read/Write' in import settings.");
                return;
            }

            Color32[] pixels = image.GetPixels32();

            // 1. 全ピクセルの輝度をリストに格納
            var brightnessList = new List<float>(pixels.Length);
            for (int i = 0; i < pixels.Length; i++)
            {
                // NTSC係数を用いた加重平均で輝度を計算
                float brightness = (pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f) / 255f;
                brightnessList.Add(brightness);
            }

            // 2. 輝度リストを降順（明るい順）にソート
            brightnessList.Sort((a, b) => b.CompareTo(a));

            // 3. 上位10%の境界となる輝度を閾値として決定
            int thresholdIndex = (int)(pixels.Length * 0.1f);
            // ピクセル数が少ない場合に備え、範囲外アクセスを防止
            if (thresholdIndex >= brightnessList.Count)
            {
                thresholdIndex = brightnessList.Count - 1;
            }
            float threshold = (thresholdIndex >= 0) ? brightnessList[thresholdIndex] : 1.0f;

            // 4. 閾値以上の輝度を持つピクセルを透明化
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
        /// <param name="image">保存対象のTexture2D</param>
        /// <param name="outputPath">保存先のフルパス</param>
        public static async Task SaveRawImageAsync(Texture2D image, string outputPath)
        {
            byte[] bytes = image.EncodeToPNG();
            await File.WriteAllBytesAsync(outputPath, bytes);
        }

        /// <summary>
        /// ステータス確定後、ファイルをStagedからConfirmedへ移動する。
        /// </summary>
        /// <param name="sourcePath">移動元のファイルパス</param>
        /// <param name="destinationPath">移動先のファイルパス</param>
        public static void MoveAndRenameConfirmedFile(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                // 移動先にディレクトリが存在しない場合は作成
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