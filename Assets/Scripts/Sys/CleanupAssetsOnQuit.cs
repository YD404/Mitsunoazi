using UnityEngine;
using System.IO;

// UnityEditor名前空間はエディタ専用APIを含むため、プリプロセッサディレクティブで囲む
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ESCキーまたはXキー3回連打でアプリケーションを終了し、終了時に特定のディレクトリ内のPNGファイルを削除します。
/// </summary>
public class CleanupAssetsOnQuit : MonoBehaviour
{
    private int _xKeyPressCount = 0;
    private float _lastXKeyPressTime = 0f;
    private const float COMBO_TIME_WINDOW = 1.5f; // コンボ判定の時間ウィンドウ（秒）
    private bool _isXComboTriggered = false;

    /// <summary>
    /// フレームごとに呼び出されるUnityのライフサイクルメソッド。
    /// </summary>
    private void Update()
    {
        // ESCキーがこのフレームで押されたかを検出
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitApplication(false);
        }

        // Xキーがこのフレームで押されたかを検出
        if (Input.GetKeyDown(KeyCode.X))
        {
            HandleXKeyCombo();
        }
    }

    /// <summary>
    /// Xキーの連打入力を処理し、3回連打で終了処理を実行します。
    /// </summary>
    private void HandleXKeyCombo()
    {
        float currentTime = Time.time;

        // 時間ウィンドウ内での連打かをチェック
        if (currentTime - _lastXKeyPressTime > COMBO_TIME_WINDOW)
        {
            // 時間切れの場合はカウントをリセット
            _xKeyPressCount = 0;
        }

        _xKeyPressCount++;
        _lastXKeyPressTime = currentTime;

        Debug.Log($"[CleanupAssetsOnQuit] X key pressed {_xKeyPressCount} time(s)");

        // 3回連打で終了処理を実行
        if (_xKeyPressCount >= 3)
        {
            _isXComboTriggered = true;
            Debug.Log("[CleanupAssetsOnQuit] X key combo detected! Triggering quit sequence.");
            QuitApplication(true);
        }
    }

    /// <summary>
    /// アプリケーションを終了する処理。エディタとビルドで挙動を分岐させる。
    /// </summary>
    /// <param name="isXCombo">Xキー連打による終了かどうか</param>
    private void QuitApplication(bool isXCombo)
    {
        Debug.Log($"Quit request received. Source: {(isXCombo ? "X Key Combo" : "ESC Key")}. Exiting application.");

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
        // 基本の削除対象ディレクトリ
        string[] targetDirectories = { "ImageCapture", "ImageStaged" };

        // Xキー連打による終了の場合はImageConfirmedも追加
        if (_isXComboTriggered)
        {
            Debug.Log("[CleanupAssetsOnQuit] X combo cleanup - including ImageConfirmed folder");
            targetDirectories = new string[] { "ImageCapture", "ImageStaged", "ImageConfirmed" };
        }
        else
        {
            Debug.Log("[CleanupAssetsOnQuit] Normal cleanup - standard folders only");
        }

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

            Debug.Log($"[CleanupAssetsOnQuit] Cleanup completed for: {directoryPath}. Deleted {pngFiles.Length} files.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CleanupAssetsOnQuit] Error deleting files in {directoryPath}. Exception: {ex.Message}");
        }
    }
}
