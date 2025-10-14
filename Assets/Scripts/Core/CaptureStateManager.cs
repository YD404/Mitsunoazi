using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Mitsunoazi
{
    public class CaptureStateManager : MonoBehaviour
    {
        public event Action<int, StatusManager.Status> OnStatusChanged;
        public event Action<int, StatusManager.Status> OnCaptureStarted;
        public event Action<int> OnConfirm;

        [Header("Dependencies")]
        [SerializeField] private TimelinePlayer timelinePlayer;

        [Header("Settings")]
        [SerializeField] private float statusSelectionTimeoutSeconds = 10.0f;

        // ★修正: カメラ状態のenumをpublicに変更
        public enum CameraStateType
        {
            Ready,
            SelectingStatus,
            Processing
        }

        private class CameraState
        {
            public CameraStateType CurrentState = CameraStateType.Ready;
            public StatusManager.Status CurrentStatus = StatusManager.Status.Crazy;
            public string BaseFileName;
            public int CameraIndex;
            public bool ConfirmationPending = false;
            // ★追加: 画像処理タスクの参照
            public Task ImageProcessingTask = null;
        }

        private CameraState[] cameraStates;
        private Coroutine[] selectionTimeoutCoroutines;
        private const int MAX_CAMERAS = 5;

        private void Start()
        {
            cameraStates = new CameraState[MAX_CAMERAS];
            selectionTimeoutCoroutines = new Coroutine[MAX_CAMERAS];

            for (int i = 0; i < MAX_CAMERAS; i++)
            {
                cameraStates[i] = new CameraState { CameraIndex = i };
            }

            if (timelinePlayer == null)
            {
                Debug.LogWarning("TimelinePlayer is not assigned in the inspector. Timeline playback will be skipped.");
            }
        }

        private void Update()
        {
            //// タイムライン再生中は状態変更を一時停止（オプション）
            //if (Director.IsTimelinePlaying)
            //{
            //    // 再生中でも特定の操作は許可するか、完全にブロックするかは仕様による
            //    // ここではデバッグログのみ出力
            //    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            //    {
            //        Debug.Log($"[CaptureStateManager] タイムライン再生中はクリック入力を受信しましたが、処理をスキップします");
            //    }
            //    return;
            //}

            for (int i = 0; i < cameraStates.Length; i++)
            {
                var state = cameraStates[i];

                // 状態ごとの処理を明確に分離
                switch (state.CurrentState)
                {
                    case CameraStateType.Ready:
                        // Ready状態では何もしない
                        break;

                    case CameraStateType.SelectingStatus:
                        ProcessSelectingState(i, state);
                        break;

                    case CameraStateType.Processing:
                        ProcessProcessingState(i, state);
                        break;
                }
            }
        }

        private void ProcessSelectingState(int cameraIndex, CameraState state)
        {
            // ステータス変更キー処理
            if (Input.GetKeyDown(KeyCode.Alpha0 + (cameraIndex * 2)) ||
                Input.GetKeyDown(KeyCode.Keypad0 + (cameraIndex * 2)))
            {
                state.CurrentStatus = StatusManager.GetNextStatus(state.CurrentStatus);
                OnStatusChanged?.Invoke(cameraIndex, state.CurrentStatus);
                ResetSelectionTimeout(cameraIndex);
                Debug.Log($"Camera {cameraIndex}: Status changed to {state.CurrentStatus}");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0 + (cameraIndex * 2 + 1)) ||
                     Input.GetKeyDown(KeyCode.Keypad0 + (cameraIndex * 2 + 1)))
            {
                state.CurrentStatus = StatusManager.GetPreviousStatus(state.CurrentStatus);
                OnStatusChanged?.Invoke(cameraIndex, state.CurrentStatus);
                ResetSelectionTimeout(cameraIndex);
                Debug.Log($"Camera {cameraIndex}: Status changed to {state.CurrentStatus}");
            }

            // マウスクリックで確定処理
            bool mouseClick = false;
            if (cameraIndex % 2 == 0)
            {
                mouseClick = Input.GetMouseButtonDown(0);
            }
            else
            {
                mouseClick = Input.GetMouseButtonDown(1);
            }

            if (mouseClick)
            {
                Debug.Log($"[CaptureStateManager] カメラ {cameraIndex} の確定処理を開始 (SelectingStatus)");
                HandleConfirm(cameraIndex);
            }
        }

        private void ProcessProcessingState(int cameraIndex, CameraState state)
        {
            // 処理中はステータス変更を許可しない

            // マウスクリックで保留フラグ設定
            bool mouseClick = false;
            if (cameraIndex % 2 == 0)
            {
                mouseClick = Input.GetMouseButtonDown(0);
            }
            else
            {
                mouseClick = Input.GetMouseButtonDown(1);
            }

            if (mouseClick)
            {
                state.ConfirmationPending = true;
                Debug.Log($"Camera {cameraIndex}: Confirmation pending.");
            }
        }

        public bool IsCameraReady(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length)
                return false;
            return cameraStates[cameraIndex].CurrentState == CameraStateType.Ready;
        }

        // ★修正: 戻り値の型をpublicなenumに変更
        public CameraStateType GetCurrentState(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length)
                return CameraStateType.Ready;
            return cameraStates[cameraIndex].CurrentState;
        }

        // 既存のメソッドを削除して、以下に置き換え
        public void NotifyCaptureStarted(int cameraIndex, StatusManager.Status currentStatus)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length) return;

            Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 状態を Ready -> Processing に変更。ステータス: {currentStatus}");
            cameraStates[cameraIndex].CurrentState = CameraStateType.Processing;
            CancelSelectionTimeout(cameraIndex);

            // キャプチャ開始イベントを発行
            OnCaptureStarted?.Invoke(cameraIndex, currentStatus);
        }
        public StatusManager.Status GetCurrentStatus(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length)
                return StatusManager.Status.Crazy;
            return cameraStates[cameraIndex].CurrentStatus;
        }

        // 既存の引数なしバージョンも保持（他のコードから呼ばれている可能性があるため）
        public void NotifyCaptureStarted(int cameraIndex)
        {
            // 現在のステータスを取得して、新しいメソッドを呼び出す
            var currentStatus = GetCurrentStatus(cameraIndex);
            NotifyCaptureStarted(cameraIndex, currentStatus);
        }

        public async void OnCaptureComplete(int cameraIndex, Texture2D capturedImage, string timestamp)
        {
            try
            {
                var state = cameraStates[cameraIndex];
                state.BaseFileName = $"webcam_{cameraIndex}_{timestamp}";

                string stagedPath = Path.Combine(Application.streamingAssetsPath, "ImageStaged", state.BaseFileName + ".png");

                Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 画像処理を開始。保存先: {stagedPath}");

                // ★変更点1: 画像処理を待たずにすぐにステータス選択状態に変更
                Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 状態を Processing -> SelectingStatus に即時変更。");
                state.CurrentState = CameraStateType.SelectingStatus;
                StartSelectionTimeout(cameraIndex);

                // ★変更点2: 画像処理をバックグラウンドで実行
                var imageProcessingTask = ImageProcessor.ProcessAndSaveImageAsync(capturedImage, stagedPath);

                // ★変更点3: タスクを状態に保存（確定処理で待機するため）
                state.ImageProcessingTask = imageProcessingTask;

                // ★変更点4: タスク完了後の処理
                _ = imageProcessingTask.ContinueWith(t =>
                {
                    // メインスレッドで実行する必要があるので、コルーチンを使用
                    StartCoroutine(OnImageProcessingCompleted(cameraIndex, capturedImage, t));
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"An error occurred during image processing for camera {cameraIndex}: {e.Message}");
                var state = cameraStates[cameraIndex];
                state.ConfirmationPending = false;
                state.CurrentState = CameraStateType.Ready;
                CancelSelectionTimeout(cameraIndex);
            }
        }

        // ★追加: 画像処理完了後の処理
        private IEnumerator OnImageProcessingCompleted(int cameraIndex, Texture2D capturedImage, Task processingTask)
        {
            // 次のフレームまで待機（メインスレッドでの実行を保証）
            yield return null;

            try
            {
                if (processingTask.IsFaulted)
                {
                    Debug.LogError($"[CaptureStateManager] カメラ {cameraIndex} の画像処理に失敗: {processingTask.Exception}");
                }
                else
                {
                    Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 画像処理完了");
                }

                Destroy(capturedImage);

                var state = cameraStates[cameraIndex];
                state.ImageProcessingTask = null; // タスク完了

                // 確定が保留中で、まだ確定されていない場合
                if (state.ConfirmationPending && state.CurrentState == CameraStateType.SelectingStatus)
                {
                    Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 保留中の確定処理を実行します（画像処理完了後）。");
                    HandleConfirm(cameraIndex);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CaptureStateManager] 画像処理完了後の処理中にエラー: {e.Message}");
            }
        }


        public void NotifyCaptureFinished(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length) return;

            var state = cameraStates[cameraIndex];
            state.CurrentState = CameraStateType.Ready;
            state.ConfirmationPending = false;
            CancelSelectionTimeout(cameraIndex);
            Debug.Log($"Camera {cameraIndex}: Capture failed or was cancelled. State has been reset to Ready.");
        }

        private async void HandleConfirm(int cameraIndex)
        {
            var state = cameraStates[cameraIndex];
            if (state.CurrentState == CameraStateType.Ready) return;

            CancelSelectionTimeout(cameraIndex);

            // ★変更点: 画像処理が完了していない場合は待機
            if (state.ImageProcessingTask != null && !state.ImageProcessingTask.IsCompleted)
            {
                Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 画像処理の完了を待ってから確定処理を実行します...");

                // 状態を一時的にProcessingに変更（ユーザーに待機を伝える）
                state.CurrentState = CameraStateType.Processing;

                try
                {
                    await state.ImageProcessingTask;
                    Debug.Log($"[CaptureStateManager] カメラ {cameraIndex}: 画像処理完了、確定処理を続行");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CaptureStateManager] カメラ {cameraIndex} の画像処理待機中にエラー: {e.Message}");
                    state.CurrentState = CameraStateType.Ready;
                    return;
                }

                // 待機後に再度状態チェック
                if (state.CurrentState == CameraStateType.Ready) return;
            }

            string stagedFileName = state.BaseFileName + ".png";
            string confirmedFileName = state.BaseFileName + $"_{state.CurrentStatus}.png";

            string stagedPath = Path.Combine(Application.streamingAssetsPath, "ImageStaged", stagedFileName);
            string confirmedPath = Path.Combine(Application.streamingAssetsPath, "ImageConfirmed", confirmedFileName);

            // ファイルが存在するか確認
            if (!File.Exists(stagedPath))
            {
                Debug.LogError($"[CaptureStateManager] カメラ {cameraIndex}: 確定処理に必要な画像ファイルが見つかりません: {stagedPath}");
                state.CurrentState = CameraStateType.Ready;
                return;
            }

            ImageProcessor.MoveAndRenameConfirmedFile(stagedPath, confirmedPath);
            Debug.Log($"Camera {cameraIndex}: Confirmed. File moved to {confirmedPath}");

            if (timelinePlayer != null)
            {
                TimelinePlayer.ClickType clickType = (cameraIndex % 2 == 0) ?
                    TimelinePlayer.ClickType.Left : TimelinePlayer.ClickType.Right;

                timelinePlayer.Play(confirmedPath, state.CurrentStatus, clickType);
            }
            OnConfirm?.Invoke(cameraIndex);

            state.CurrentState = CameraStateType.Ready;
            state.CurrentStatus = StatusManager.Status.Crazy;
            state.ConfirmationPending = false;
            state.ImageProcessingTask = null; // タスク参照をクリア
        }

        // === タイムアウト管理メソッド ===
        private void StartSelectionTimeout(int cameraIndex)
        {
            CancelSelectionTimeout(cameraIndex);
            selectionTimeoutCoroutines[cameraIndex] = StartCoroutine(SelectionTimeoutRoutine(cameraIndex));
            Debug.Log($"[CaptureStateManager] カメラ {cameraIndex} のタイムアウト監視を開始: {statusSelectionTimeoutSeconds}秒");
        }

        private void ResetSelectionTimeout(int cameraIndex)
        {
            if (cameraStates[cameraIndex].CurrentState == CameraStateType.SelectingStatus)
            {
                StartSelectionTimeout(cameraIndex);
            }
        }

        private void CancelSelectionTimeout(int cameraIndex)
        {
            if (selectionTimeoutCoroutines[cameraIndex] != null)
            {
                StopCoroutine(selectionTimeoutCoroutines[cameraIndex]);
                selectionTimeoutCoroutines[cameraIndex] = null;
                Debug.Log($"[CaptureStateManager] カメラ {cameraIndex} のタイムアウト監視をキャンセル");
            }
        }

        private IEnumerator SelectionTimeoutRoutine(int cameraIndex)
        {
            yield return new WaitForSeconds(statusSelectionTimeoutSeconds);

            // タイムアウト発生：状態を自動リセット
            if (cameraStates[cameraIndex].CurrentState == CameraStateType.SelectingStatus)
            {
                Debug.LogWarning($"Camera {cameraIndex}: Status selection timeout. Resetting to ready state.");
                cameraStates[cameraIndex].CurrentState = CameraStateType.Ready;
                cameraStates[cameraIndex].CurrentStatus = StatusManager.Status.Crazy;
                cameraStates[cameraIndex].ConfirmationPending = false;
            }
        }

        // === デバッグ用公開メソッド ===
        public void ForceResetCameraState(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length) return;

            var state = cameraStates[cameraIndex];
            state.CurrentState = CameraStateType.Ready;
            state.CurrentStatus = StatusManager.Status.Crazy;
            state.ConfirmationPending = false;
            CancelSelectionTimeout(cameraIndex);

            Debug.LogWarning($"カメラ {cameraIndex} の状態を強制リセットしました");
        }

        public void ResetAllCameraStates()
        {
            for (int i = 0; i < cameraStates.Length; i++)
            {
                ForceResetCameraState(i);
            }
            Debug.Log("すべてのカメラ状態をリセットしました");
        }

        public string GetCameraStateDebugInfo(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length)
                return $"カメラ {cameraIndex}: 無効なインデックス";

            var state = cameraStates[cameraIndex];
            string timeoutInfo = selectionTimeoutCoroutines[cameraIndex] != null ? "監視中" : "未監視";

            return $"カメラ {cameraIndex}: 状態={state.CurrentState}, ステータス={state.CurrentStatus}, " +
                   $"保留中={state.ConfirmationPending}, タイムアウト={timeoutInfo}";
        }

        public void LogAllCameraStates()
        {
            Debug.Log("=== 全カメラ状態デバッグ情報 ===");
            for (int i = 0; i < cameraStates.Length; i++)
            {
                Debug.Log(GetCameraStateDebugInfo(i));
            }
            Debug.Log("================================");
        }
    }
}