using UnityEngine;
using System.IO;

// UnityEditor名前空間はエディタ専用APIを含むため、プリプロセッサディレクティブで囲む
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ESCキーでアプリケーションを終了し、終了時に特定のディレクトリ内のPNGファイルを削除します。
/// </summary>
public class CleanupAssetsOnQuit : MonoBehaviour
{
    /// <summary>
    /// フレームごとに呼び出されるUnityのライフサイクルメソッド。
    /// </summary>
    private void Update()
    {
        // ESCキーがこのフレームで押されたかを検出
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitApplication();
        }
    }
    
    /// <summary>
    /// アプリケーションを終了する処理。エディタとビルドで挙動を分岐させる。
    /// </summary>
    private void QuitApplication()
    {
        Debug.Log("Quit request received. Exiting application.");

#if UNITY_EDITOR
        // Unityエディタで実行中の場合、プレイモードを停止
        EditorApplication.isPlaying = false;
#else
        // ビルドされたアプリケーションで実行中の場合、アプリケーションを終了
        Application.Quit();
#endif
    }

    /// <summary>
    /// アプリケーションが終了する直前に呼び出されるUnityのライフサイクルメソッド。
    /// </summary>
    private void OnApplicationQuit()
    {
        // 削除対象とするディレクトリ名の配列
        string[] targetDirectories = { "ImageCapture", "ImageStaged" };

        foreach (string dirName in targetDirectories)
        {
            // StreamingAssetsフォルダを基準とした絶対パスを構築
            string fullPath = Path.Combine(Application.streamingAssetsPath, dirName);
            DeletePngFiles(fullPath);
        }
    }

    /// <summary>
    /// 指定されたディレクトリパス内に存在する全ての.pngファイルを削除します。
    /// </summary>
    /// <param name="directoryPath">対象のディレクトリパス</param>
    private void DeletePngFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Debug.LogWarning($"[CleanupAssetsOnQuit] Directory not found. Skipping: {directoryPath}");
            return;
        }

        try
        {
            string[] pngFiles = Directory.GetFiles(directoryPath, "*.png");
            foreach (string filePath in pngFiles)
            {
                File.Delete(filePath);
                Debug.Log($"[CleanupAssetsOnQuit] Deleted file: {filePath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CleanupAssetsOnQuit] Error deleting files in {directoryPath}. Exception: {ex.Message}");
        }
    }
}
