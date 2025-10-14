using UnityEngine;
using System.Text;

namespace Mitsunoazi
{
    public class CameraSystemDebugger : MonoBehaviour
    {
        [Header("Debug Targets")]
        [SerializeField] private WebcamCaptureManager webcamManager;
        [SerializeField] private CaptureStateManager stateManager;
        [SerializeField] private StatusDisplayController statusDisplay;
        [SerializeField] private IdleVideoController idleVideoController;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugKeys = true;
        [SerializeField] private bool autoLogOnStateChange = false;

        private StringBuilder debugLogBuilder = new StringBuilder();
        private float lastDebugLogTime = 0f;
        private const float DEBUG_LOG_INTERVAL = 2f; // デバッグログの最小間隔

        private void Start()
        {
            if (webcamManager == null)
                webcamManager = FindObjectOfType<WebcamCaptureManager>();

            if (stateManager == null)
                stateManager = FindObjectOfType<CaptureStateManager>();

            if (statusDisplay == null)
                statusDisplay = FindObjectOfType<StatusDisplayController>();

            if (idleVideoController == null)
                idleVideoController = FindObjectOfType<IdleVideoController>();

            Debug.Log("CameraSystemDebugger: デバッグシステムを初期化しました");
            LogSystemStatus();
        }

        private void Update()
        {
            if (!enableDebugKeys) return;

            // F1: システム状態の概要を表示
            if (Input.GetKeyDown(KeyCode.F1))
            {
                LogSystemStatus();
            }

            // F2: すべてのカメラ状態をリセット
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (stateManager != null)
                {
                    stateManager.ResetAllCameraStates();
                    Debug.LogWarning("デバッグ: すべてのカメラ状態を手動でリセットしました");
                }
            }

            // F3: 詳細なデバイス情報を表示
            if (Input.GetKeyDown(KeyCode.F3))
            {
                LogDetailedDeviceInfo();
            }

            // F4: カメラ状態の詳細を表示
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (stateManager != null)
                {
                    stateManager.LogAllCameraStates();
                }
            }

            // F5: 強制的にカメラ0をリセット
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (stateManager != null)
                {
                    stateManager.ForceResetCameraState(0);
                    Debug.LogWarning("デバッグ: カメラ0を強制リセットしました");
                }
            }

            // F6: 強制的にカメラ1をリセット
            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (stateManager != null)
                {
                    stateManager.ForceResetCameraState(1);
                    Debug.LogWarning("デバッグ: カメラ1を強制リセットしました");
                }
            }

            // 定期的な自動ログ（状態変化の検出用）
            if (autoLogOnStateChange && Time.time - lastDebugLogTime > DEBUG_LOG_INTERVAL)
            {
                LogQuickStatus();
                lastDebugLogTime = Time.time;
            }
        }

        private void LogSystemStatus()
        {
            debugLogBuilder.Clear();
            debugLogBuilder.AppendLine("=== カメラシステムデバッグ情報 ===");

            // WebcamCaptureManagerの状態
            if (webcamManager != null)
            {
                debugLogBuilder.AppendLine("○ WebcamCaptureManager: 接続済み");
                webcamManager.LogCurrentDeviceStatus();
            }
            else
            {
                debugLogBuilder.AppendLine("× WebcamCaptureManager: 未接続");
            }

            // CaptureStateManagerの状態
            if (stateManager != null)
            {
                debugLogBuilder.AppendLine("○ CaptureStateManager: 接続済み");
                for (int i = 0; i < 2; i++) // カメラ0,1のみ表示
                {
                    debugLogBuilder.AppendLine($"  - {stateManager.GetCameraStateDebugInfo(i)}");
                }
            }
            else
            {
                debugLogBuilder.AppendLine("× CaptureStateManager: 未接続");
            }

            // その他のコンポーネント状態
            debugLogBuilder.AppendLine($"○ StatusDisplayController: {(statusDisplay != null ? "接続済み" : "未接続")}");
            debugLogBuilder.AppendLine($"○ IdleVideoController: {(idleVideoController != null ? "接続済み" : "未接続")}");
            debugLogBuilder.AppendLine($"○ Timeline再生中: {Director.IsTimelinePlaying}");

            debugLogBuilder.AppendLine("=== デバッグキー一覧 ===");
            debugLogBuilder.AppendLine("F1: システム状態 / F2: 全リセット / F3: デバイス情報");
            debugLogBuilder.AppendLine("F4: 状態詳細 / F5: カメラ0リセット / F6: カメラ1リセット");
            debugLogBuilder.AppendLine("==============================");

            Debug.Log(debugLogBuilder.ToString());
        }

        private void LogDetailedDeviceInfo()
        {
            debugLogBuilder.Clear();
            debugLogBuilder.AppendLine("=== 詳細デバイス情報 ===");

            if (webcamManager != null)
            {
                // 固定デバイス情報
                debugLogBuilder.AppendLine("【固定デバイスリスト】");
                webcamManager.LogCurrentDeviceStatus();
            }

            // 現在のUnityが認識しているデバイス
            var currentDevices = UnityEngine.WebCamTexture.devices;
            debugLogBuilder.AppendLine("【現在のUnityデバイスリスト】");
            for (int i = 0; i < currentDevices.Length; i++)
            {
                debugLogBuilder.AppendLine($"  [{i}] {currentDevices[i].name} - {currentDevices[i].isFrontFacing}");
            }

            debugLogBuilder.AppendLine("【キーマッピング】");
            debugLogBuilder.AppendLine("  カメラ0: 0キー(正順), 1キー(逆順) + 左クリック(確定)");
            debugLogBuilder.AppendLine("  カメラ1: 2キー(正順), 3キー(逆順) + 右クリック(確定)");
            debugLogBuilder.AppendLine("==========================");

            Debug.Log(debugLogBuilder.ToString());
        }

        private void LogQuickStatus()
        {
            bool hasBusyCamera = false;

            for (int i = 0; i < 2; i++) // カメラ0,1のみチェック
            {
                if (stateManager != null && !stateManager.IsCameraReady(i))
                {
                    hasBusyCamera = true;
                    break;
                }
            }

            if (hasBusyCamera)
            {
                Debug.Log($"[自動デバッグ] 状態: カメラビジー | Timeline: {Director.IsTimelinePlaying}");
            }
        }

        // 外部から呼び出すためのデバッグメソッド
        public void ForceDeviceRecheck()
        {
            if (webcamManager != null)
            {
                webcamManager.LogCurrentDeviceStatus();
                Debug.Log("デバイス状態を強制再チェックしました");
            }
        }

        public void SimulateCameraCapture(int cameraIndex)
        {
            Debug.Log($"デバッグ: カメラ{cameraIndex}のキャプチャをシミュレート");
            // ここにシミュレーション処理を追加可能
        }

        // イベントハンドラとして登録可能なメソッド
        public void OnStatusChanged(int cameraIndex, StatusManager.Status status)
        {
            if (autoLogOnStateChange)
            {
                Debug.Log($"[状態変化] カメラ{cameraIndex}: {status}");
            }
        }

        public void OnConfirm(int cameraIndex)
        {
            if (autoLogOnStateChange)
            {
                Debug.Log($"[確定] カメラ{cameraIndex} 確定処理完了");
            }
        }
    }
}