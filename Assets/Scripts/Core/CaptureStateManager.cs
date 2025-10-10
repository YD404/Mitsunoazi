using UnityEngine;
using System.IO;
using System;

namespace Mitsunoazi
{
    public class CaptureStateManager : MonoBehaviour
    {
        public event Action<int, StatusManager.Status> OnStatusChanged;
        public event Action<int> OnConfirm;

        [Header("Dependencies")]
        [SerializeField] private TimelinePlayer timelinePlayer;

        private class CameraState
        {
            public enum State { Ready, SelectingStatus, Processing }
            public State CurrentState = State.Ready;
            public StatusManager.Status CurrentStatus = StatusManager.Status.Crazy;
            public string BaseFileName;
            public int CameraIndex;
            public bool ConfirmationPending = false;
        }

        private CameraState[] cameraStates;
        private const int MAX_CAMERAS = 5;

        private void Start()
        {
            cameraStates = new CameraState[MAX_CAMERAS];
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
            for (int i = 0; i < cameraStates.Length; i++)
            {
                var state = cameraStates[i];
                if (state.CurrentState == CameraState.State.Ready) continue;

                if (state.CurrentState == CameraState.State.SelectingStatus || state.CurrentState == CameraState.State.Processing)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + (i * 2)) || Input.GetKeyDown(KeyCode.Keypad0 + (i * 2)))
                    {
                        state.CurrentStatus = StatusManager.GetNextStatus(state.CurrentStatus);
                        OnStatusChanged?.Invoke(i, state.CurrentStatus); 
                        Debug.Log($"Camera {i}: Status changed to {state.CurrentStatus}");
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha0 + (i * 2 + 1)) || Input.GetKeyDown(KeyCode.Keypad0 + (i * 2 + 1)))
                    {
                        state.CurrentStatus = StatusManager.GetPreviousStatus(state.CurrentStatus);
                        OnStatusChanged?.Invoke(i, state.CurrentStatus); 
                        Debug.Log($"Camera {i}: Status changed to {state.CurrentStatus}");
                    }
                }

                // マウスクリックで確認処理
                bool mouseClick = false;
                if (i % 2 == 0) // カメラ0,2,4: 左クリック
                {
                    mouseClick = Input.GetMouseButtonDown(0);
                }
                else // カメラ1,3: 右クリック
                {
                    mouseClick = Input.GetMouseButtonDown(1);
                }

                if (mouseClick)
                {
                    if (state.CurrentState == CameraState.State.SelectingStatus)
                    {
                        HandleConfirm(i);
                    }
                    else if (state.CurrentState == CameraState.State.Processing)
                    {
                        state.ConfirmationPending = true;
                        Debug.Log($"Camera {i}: Confirmation pending.");
                    }
                }
            }
        }

        public bool IsCameraReady(int cameraIndex) => cameraStates[cameraIndex].CurrentState == CameraState.State.Ready;

        public void NotifyCaptureStarted(int cameraIndex) => cameraStates[cameraIndex].CurrentState = CameraState.State.Processing;
        
        public async void OnCaptureComplete(int cameraIndex, Texture2D capturedImage, string timestamp)
        {
            try
            {
                var state = cameraStates[cameraIndex];
                state.BaseFileName = $"webcam_{cameraIndex}_{timestamp}";
                
                string stagedPath = Path.Combine(Application.streamingAssetsPath, "ImageStaged", state.BaseFileName + ".png");

                await ImageProcessor.ProcessAndSaveImageAsync(capturedImage, stagedPath);
                Destroy(capturedImage);
                
                Debug.Log($"Camera {cameraIndex}: Processing complete.");

                if (state.ConfirmationPending)
                {
                    Debug.Log($"Camera {cameraIndex}: Executing pending confirmation.");
                    HandleConfirm(cameraIndex);
                }
                else
                {
                    Debug.Log($"Camera {cameraIndex}: Now in status selection mode.");
                    state.CurrentState = CameraState.State.SelectingStatus;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"An error occurred during image processing for camera {cameraIndex}: {e.Message}");
                var state = cameraStates[cameraIndex];
                state.ConfirmationPending = false;
                state.CurrentState = CameraState.State.Ready;
            }
        }

        public void NotifyCaptureFinished(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameraStates.Length) return;
    
            var state = cameraStates[cameraIndex];
            state.CurrentState = CameraState.State.Ready;
            state.ConfirmationPending = false;
            Debug.Log($"Camera {cameraIndex}: Capture failed or was cancelled. State has been reset to Ready.");
        }

        private void HandleConfirm(int cameraIndex)
        {
            var state = cameraStates[cameraIndex];
            if (state.CurrentState == CameraState.State.Ready) return;

            string stagedFileName = state.BaseFileName + ".png";
            string confirmedFileName = state.BaseFileName + $"_{state.CurrentStatus}.png";
    
            string stagedPath = Path.Combine(Application.streamingAssetsPath, "ImageStaged", stagedFileName);
            string confirmedPath = Path.Combine(Application.streamingAssetsPath, "ImageConfirmed", confirmedFileName);
    
            ImageProcessor.MoveAndRenameConfirmedFile(stagedPath, confirmedPath);
            Debug.Log($"Camera {cameraIndex}: Confirmed. File moved to {confirmedPath}");
    
            if (timelinePlayer != null)
            {
                // カメラインデックスからクリックタイプを判定
                TimelinePlayer.ClickType clickType = (cameraIndex % 2 == 0) ? 
                    TimelinePlayer.ClickType.Left : TimelinePlayer.ClickType.Right;
        
                timelinePlayer.Play(confirmedPath, state.CurrentStatus, clickType);
            }
            OnConfirm?.Invoke(cameraIndex);
    
            state.CurrentState = CameraState.State.Ready;
            state.CurrentStatus = StatusManager.Status.Crazy;
            state.ConfirmationPending = false; 
        }
    }
}
