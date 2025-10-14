using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using Mitsunoazi;

public class WebcamCaptureManager : MonoBehaviour
{
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

    private bool[] _isCapturingByCamera;
    private const int MAX_CAMERAS = 5;

    // ★追加: 起動時に固定化するデバイスリスト
    private WebCamDevice[] _fixedDevices;
    private Dictionary<int, string> _cameraIndexToDeviceName;

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

        // ★追加: デバイス列挙の固定化
        InitializeFixedDevices();
    }

    // ★追加: 起動時にデバイスを固定化
    private void InitializeFixedDevices()
    {
        _fixedDevices = WebCamTexture.devices;
        _cameraIndexToDeviceName = new Dictionary<int, string>();

        if (_fixedDevices.Length == 0)
        {
            Debug.LogWarning("カメラが接続されていません。");
            return;
        }

        // デバイス情報をログ出力
        Debug.Log("=== 固定化されたカメラデバイスリスト ===");
        for (int i = 0; i < _fixedDevices.Length; i++)
        {
            Debug.Log($"固定カメラ[{i}]: {_fixedDevices[i].name} (使用キー: {i * 2}, {i * 2 + 1})");

            // カメラインデックスとデバイス名のマッピングを保存
            if (i < MAX_CAMERAS)
            {
                _cameraIndexToDeviceName[i] = _fixedDevices[i].name;
            }
        }
        Debug.Log("=====================================");

        // 利用可能なカメラ数より多くのキーマッピングが定義されていないかチェック
        for (int i = _fixedDevices.Length; i < MAX_CAMERAS; i++)
        {
            Debug.LogWarning($"カメラインデックス {i} は利用可能なデバイス数({_fixedDevices.Length})を超えています。");
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

                // ★修正: カメラインデックスの有効性チェック
                if (cameraIndex >= _fixedDevices.Length)
                {
                    Debug.LogWarning($"カメラインデックス {cameraIndex} は利用可能なデバイス範囲外です。最大: {_fixedDevices.Length - 1}");
                    break;
                }

                Debug.Log($"[WebcamCaptureManager] KeyDown: {mapping.Key}, Target Camera: {cameraIndex}, Device: {_fixedDevices[cameraIndex].name}");

                // ★修正: 状態チェックを新しいenum名に合わせる
                bool isReady = captureStateManager.IsCameraReady(cameraIndex);
                bool isNotCapturing = !_isCapturingByCamera[cameraIndex];
                bool isNotProcessing = captureStateManager.GetCurrentState(cameraIndex) == CaptureStateManager.CameraStateType.Ready;

                if (isReady && isNotCapturing && isNotProcessing)
                {
                    Debug.Log($"[WebcamCaptureManager] カメラ {cameraIndex} はキャプチャ可能です。処理を開始します。");
                    StartCoroutine(CaptureWebcam(cameraIndex));
                }
                else
                {
                    Debug.LogWarning($"[WebcamCaptureManager] カメラ {cameraIndex} はキャプチャできません。IsReady: {isReady}, IsCapturing: {_isCapturingByCamera[cameraIndex]}, State: {captureStateManager.GetCurrentState(cameraIndex)}");
                }
                break;
            }
        }
    }

    private IEnumerator CaptureWebcam(int cameraIndex)
    {
        _isCapturingByCamera[cameraIndex] = true;

        // ★追加: キャプチャ開始通知（ステータスUI表示のトリガー）
        var currentStatus = captureStateManager.GetCurrentStatus(cameraIndex);
        captureStateManager.NotifyCaptureStarted(cameraIndex, currentStatus);

        Debug.Log($"[WebcamCaptureManager] カメラ {cameraIndex}: 状態を Ready -> Processing に変更。");

        // ★修正: 固定化されたデバイスリストを使用
        if (cameraIndex >= _fixedDevices.Length)
        {
            Debug.LogWarning($"カメラ番号 {cameraIndex} は見つかりませんでした。");
            _isCapturingByCamera[cameraIndex] = false;
            captureStateManager.NotifyCaptureFinished(cameraIndex);
            yield break;
        }

        // ★修正: 固定化されたデバイス名を使用
        string deviceName = _fixedDevices[cameraIndex].name;
        Debug.Log($"[WebcamCaptureManager] カメラ {cameraIndex} のデバイス '{deviceName}' を起動します");

        WebCamTexture webCamTexture = new WebCamTexture(deviceName, CAPTURE_WIDTH, CAPTURE_HEIGHT);
        webCamTexture.Play();

        // 既存の待機処理...
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
                captureStateManager.NotifyCaptureFinished(cameraIndex);
                yield break;
            }
            if (webCamTexture.didUpdateThisFrame) stableFramesCount++;
            yield return null;
        }

        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        texture.SetPixels(webCamTexture.GetPixels());
        texture.Apply();

        webCamTexture.Stop();

        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string baseFileName = $"webcam_{cameraIndex}_{timestamp}.png";

        string capturePath = Path.Combine(Application.streamingAssetsPath, "ImageCapture", baseFileName);
        Task saveRawTask = ImageProcessor.SaveRawImageAsync(texture, capturePath);

        captureStateManager.OnCaptureComplete(cameraIndex, texture, timestamp);

        while (!saveRawTask.IsCompleted)
        {
            yield return null;
        }
        Debug.Log($"生画像を保存しました: {capturePath}");

        _isCapturingByCamera[cameraIndex] = false;
    }

    // ★追加: デバイス情報のデバッグ用メソッド
    public void LogCurrentDeviceStatus()
    {
        Debug.Log("=== 現在のデバイス状態 ===");
        var currentDevices = WebCamTexture.devices;
        Debug.Log($"固定デバイス数: {_fixedDevices.Length}, 現在のデバイス数: {currentDevices.Length}");

        for (int i = 0; i < Mathf.Max(_fixedDevices.Length, currentDevices.Length); i++)
        {
            string fixedName = i < _fixedDevices.Length ? _fixedDevices[i].name : "N/A";
            string currentName = i < currentDevices.Length ? currentDevices[i].name : "N/A";

            if (fixedName != currentName)
            {
                Debug.LogWarning($"デバイス[{i}] 固定: {fixedName} ≠ 現在: {currentName} ← 不一致!");
            }
            else
            {
                Debug.Log($"デバイス[{i}] 固定: {fixedName} = 現在: {currentName}");
            }
        }
    }
}