using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Mitsunoazi
{
    public class StatusDisplayController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private CaptureStateManager captureStateManager;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private IdleVideoController idleVideoController;

        [Header("UI Elements")]
        [SerializeField] private Image statusImageLeft;
        [SerializeField] private Image statusImageRight;

        [Header("Settings")]
        [SerializeField] private float displayTimeoutSeconds = 5.0f;

        [Header("Status Assets")]
        [SerializeField] private AudioClip commonStatusChangeClip;
        [SerializeField] private AudioClip confirmationClip;
        [SerializeField] private StatusUIData[] statusUIDataArray;

        private Coroutine leftTimeoutCoroutine;
        private Coroutine rightTimeoutCoroutine;

        [Serializable]
        public class StatusUIData
        {
            public StatusManager.Status status;
            public Sprite sprite;
        }

        private void Start()
        {
            if (statusImageLeft != null) statusImageLeft.gameObject.SetActive(false);
            if (statusImageRight != null) statusImageRight.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (captureStateManager != null)
            {
                captureStateManager.OnStatusChanged += HandleStatusChange;
                captureStateManager.OnConfirm += HandleConfirm;
                // ★追加: キャプチャ開始イベントを購読
                captureStateManager.OnCaptureStarted += HandleCaptureStarted;
            }
        }

        private void OnDisable()
        {
            if (captureStateManager != null)
            {
                captureStateManager.OnStatusChanged -= HandleStatusChange;
                captureStateManager.OnConfirm -= HandleConfirm;
                // ★追加: キャプチャ開始イベントの購読解除
                captureStateManager.OnCaptureStarted -= HandleCaptureStarted;
            }
        }

        // ★追加: キャプチャ開始時の処理
        private void HandleCaptureStarted(int cameraIndex, StatusManager.Status currentStatus)
        {
            Debug.Log($"[StatusDisplayController] カメラ {cameraIndex} のキャプチャ開始 - ステータス: {currentStatus}");

            // アイドル動画コントローラーにタイマーリセットを通知
            if (idleVideoController != null)
            {
                idleVideoController.ResetIdleTimer(cameraIndex);
            }

            // ステータスUIを表示（既存の表示ロジックを再利用）
            bool isEven = cameraIndex % 2 == 0;
            Image targetImage = isEven ? statusImageLeft : statusImageRight;

            // タイムアウトコルーチンの管理
            if (isEven)
            {
                if (leftTimeoutCoroutine != null) StopCoroutine(leftTimeoutCoroutine);
                leftTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }
            else
            {
                if (rightTimeoutCoroutine != null) StopCoroutine(rightTimeoutCoroutine);
                rightTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }

            // 現在のステータスに対応するスプライトを設定
            Sprite targetSprite = GetSpriteForStatus(currentStatus);
            if (targetImage != null && targetSprite != null)
            {
                targetImage.sprite = targetSprite;
                targetImage.gameObject.SetActive(true);
                Debug.Log($"[StatusDisplayController] キャプチャ開始でステータスUI表示: {currentStatus}");
            }
        }

        private void HandleStatusChange(int cameraIndex, StatusManager.Status newStatus)
        {
            if (audioSource != null && commonStatusChangeClip != null)
            {
                audioSource.PlayOneShot(commonStatusChangeClip);
            }

            if (idleVideoController != null)
            {
                idleVideoController.ResetIdleTimer(cameraIndex);
            }

            bool isEven = cameraIndex % 2 == 0;
            Image targetImage = isEven ? statusImageLeft : statusImageRight;

            if (isEven)
            {
                if (leftTimeoutCoroutine != null) StopCoroutine(leftTimeoutCoroutine);
                leftTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }
            else
            {
                if (rightTimeoutCoroutine != null) StopCoroutine(rightTimeoutCoroutine);
                rightTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }

            Sprite targetSprite = GetSpriteForStatus(newStatus);
            if (targetImage != null && targetSprite != null)
            {
                targetImage.sprite = targetSprite;
                targetImage.gameObject.SetActive(true);
            }
        }

        private void HandleConfirm(int cameraIndex)
        {
            Debug.Log($"[StatusDisplayController] カメラ {cameraIndex} の確定イベント受信");

            if (idleVideoController != null)
            {
                idleVideoController.CancelIdleState(cameraIndex);
            }

            if (audioSource != null && confirmationClip != null)
            {
                audioSource.PlayOneShot(confirmationClip);
                Debug.Log($"[StatusDisplayController] 確定音再生");
            }

            Director.IsTimelinePlaying = true;
            Debug.Log($"[StatusDisplayController] タイムライン再生フラグをONに設定");

            bool isEven = cameraIndex % 2 == 0;
            if (isEven)
            {
                if (leftTimeoutCoroutine != null)
                {
                    StopCoroutine(leftTimeoutCoroutine);
                    Debug.Log($"[StatusDisplayController] 左側タイマー停止");
                }
                if (statusImageLeft != null)
                {
                    statusImageLeft.gameObject.SetActive(false);
                    Debug.Log($"[StatusDisplayController] 左側画像非表示");
                }
            }
            else
            {
                if (rightTimeoutCoroutine != null)
                {
                    StopCoroutine(rightTimeoutCoroutine);
                    Debug.Log($"[StatusDisplayController] 右側タイマー停止");
                }
                if (statusImageRight != null)
                {
                    statusImageRight.gameObject.SetActive(false);
                    Debug.Log($"[StatusDisplayController] 右側画像非表示");
                }
            }
        }

        // ★追加: ステータスからスプライトを取得するヘルパーメソッド
        private Sprite GetSpriteForStatus(StatusManager.Status status)
        {
            foreach (var data in statusUIDataArray)
            {
                if (data.status == status)
                {
                    return data.sprite;
                }
            }
            return null;
        }

        private IEnumerator ImageTimeoutRoutine(Image imageToHide)
        {
            yield return new WaitForSeconds(displayTimeoutSeconds);

            if (imageToHide != null)
            {
                imageToHide.gameObject.SetActive(false);
            }
        }
    }
}