using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using Mitsunoazi; // <-- この行を追加

/// <summary>
/// ウェブカメラの入力検知と映像のキャプチャ、生画像の保存を管理する。
/// </summary>
public class WebcamCaptureManager : MonoBehaviour
{
    // ★追加: CaptureStateManagerへの参照 (Unityエディタ上で設定)
    [SerializeField] private CaptureStateManager captureStateManager;

    private readonly Dictionary<KeyCode, int> _cameraKeyMappings = new Dictionary<KeyCode, int>
    {
        { KeyCode.Alpha0, 0 }, { KeyCode.Alpha1, 0 },
        { KeyCode.Alpha2, 1 }, { KeyCode.Alpha3, 1 },
        { KeyCode.Alpha4, 2 }, { KeyCode.Alpha5, 2 },
        { KeyCode.Alpha6, 3 }, { KeyCode.Alpha7, 3 },
        { KeyCode.Alpha8, 4 }, { KeyCode.Alpha9, 4 }
    };

    private const int CAPTURE_WIDTH = 640;
    private const int CAPTURE_HEIGHT = 360;
    
    // カメラごとのキャプチャ処理中フラグ
    private bool[] _isCapturingByCamera;
    private const int MAX_CAMERAS = 5;

    private void Start()
    {
        _isCapturingByCamera = new bool[MAX_CAMERAS];
        
        if (captureStateManager == null)
        {
            Debug.LogError("CaptureStateManagerがインスペクターで設定されていません。");
        }
        
        // 各フォルダが存在しない場合は作成
        Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "ImageCapture"));
        Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "ImageStaged"));
        Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "ImageConfirmed"));

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("カメラが接続されていません。");
        }
        else
        {
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"利用可能なカメラ[{i}]: {devices[i].name}");
            }
        }
    }

    private void Update()
    {
        if (captureStateManager == null) return;

        foreach (var mapping in _cameraKeyMappings)
        {
            if (Input.GetKeyDown(mapping.Key))
            {
                int cameraIndex = mapping.Value;
                
                // ★修正: CaptureStateManagerに状態を問い合わせ
                if (captureStateManager.IsCameraReady(cameraIndex) && !_isCapturingByCamera[cameraIndex])
                {
                    StartCoroutine(CaptureWebcam(cameraIndex));
                }
                break;
            }
        }
    }
    
    private IEnumerator CaptureWebcam(int cameraIndex)
    {
        _isCapturingByCamera[cameraIndex] = true;
        // ★追加: 状態変化を通知
        captureStateManager.NotifyCaptureStarted(cameraIndex);

        WebCamDevice[] devices = WebCamTexture.devices;

        if (cameraIndex >= devices.Length)
        {
            Debug.LogWarning($"カメラ番号 {cameraIndex} は見つかりませんでした。");
            _isCapturingByCamera[cameraIndex] = false;
            captureStateManager.NotifyCaptureFinished(cameraIndex); // 状態を元に戻す
            yield break;
        }

        WebCamTexture webCamTexture = new WebCamTexture(devices[cameraIndex].name, CAPTURE_WIDTH, CAPTURE_HEIGHT);
        webCamTexture.Play();

        // 映像が安定するまで待機
        const int requiredStableFrames = 30;
        int stableFramesCount = 0;
        const float timeout = 3.0f;
        float startTime = Time.time;

        while (stableFramesCount < requiredStableFrames)
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogWarning($"カメラ {cameraIndex} の初期化がタイムアウトしました。");
                webCamTexture.Stop();
                _isCapturingByCamera[cameraIndex] = false;
                captureStateManager.NotifyCaptureFinished(cameraIndex); // 状態を元に戻す
                yield break;
            }
            if (webCamTexture.didUpdateThisFrame) stableFramesCount++;
            yield return null;
        }
        
        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        texture.SetPixels(webCamTexture.GetPixels());
        texture.Apply();

        webCamTexture.Stop();

        // --- ★ここからが処理フローの主要な変更点 ---
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string baseFileName = $"webcam_{cameraIndex}_{timestamp}.png";
        
        // 1. 生画像をImageCaptureに非同期で保存
        string capturePath = Path.Combine(Application.streamingAssetsPath, "ImageCapture", baseFileName);
        Task saveRawTask = ImageProcessor.SaveRawImageAsync(texture, capturePath);
        
        // 2. 処理をCaptureStateManagerに非同期で委譲
        captureStateManager.OnCaptureComplete(cameraIndex, texture, timestamp);
        
        // 生画像の保存タスクの完了を待機 (待たなくても良いが、念のため)
        while (!saveRawTask.IsCompleted)
        {
            yield return null;
        }
        Debug.Log($"生画像を保存しました: {capturePath}");

        _isCapturingByCamera[cameraIndex] = false;
    }
}
