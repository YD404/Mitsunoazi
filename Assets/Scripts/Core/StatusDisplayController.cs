// Assets/Scripts/Sys/StatusDisplayController.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Mitsunoazi
{
    public class StatusDisplayController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField]
        private CaptureStateManager captureStateManager;
        [SerializeField]
        private AudioSource audioSource;
        
        [Header("UI Elements")]
        [Tooltip("カメラIndexが偶数の時に使用するImage")]
        [SerializeField]
        private Image statusImageLeft;
        [Tooltip("カメラIndexが奇数の時に使用するImage")]
        [SerializeField]
        private Image statusImageRight;

        [Header("Settings")]
        [Tooltip("非表示になるまでの時間（秒）")]
        [SerializeField]
        private float displayTimeoutSeconds = 5.0f;

        [Header("Status Assets")]
        [SerializeField]
        private AudioClip commonStatusChangeClip;
        [SerializeField]
        private StatusUIData[] statusUIDataArray; 

        // 左右それぞれのタイマー（コルーチン）を管理する変数
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
            }
        }

        private void OnDisable()
        {
            if (captureStateManager != null)
            {
                captureStateManager.OnStatusChanged -= HandleStatusChange;
                captureStateManager.OnConfirm -= HandleConfirm;
            }
        }

        private void HandleStatusChange(int cameraIndex, StatusManager.Status newStatus)
        {
            if (audioSource != null && commonStatusChangeClip != null)
            {
                audioSource.PlayOneShot(commonStatusChangeClip);
            }

            // cameraIndexの偶数/奇数によって対象Imageとコルーチンを決定
            bool isEven = cameraIndex % 2 == 0;
            Image targetImage = isEven ? statusImageLeft : statusImageRight;

            // 該当する側のタイマーをリセットして、新しいタイマーを開始
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
            
            Sprite targetSprite = null;
            foreach (var data in statusUIDataArray)
            {
                if (data.status == newStatus)
                {
                    targetSprite = data.sprite;
                    break;
                }
            }

            if (targetImage != null)
            {
                targetImage.sprite = targetSprite;
                targetImage.gameObject.SetActive(true); // 表示を有効化
            }
        }

        private void HandleConfirm(int cameraIndex)
        {
            // cameraIndexに応じて対象を絞り、タイマーを停止して非表示にする
            bool isEven = cameraIndex % 2 == 0;
            if (isEven)
            {
                if (leftTimeoutCoroutine != null) StopCoroutine(leftTimeoutCoroutine);
                if (statusImageLeft != null) statusImageLeft.gameObject.SetActive(false);
            }
            else
            {
                if (rightTimeoutCoroutine != null) StopCoroutine(rightTimeoutCoroutine);
                if (statusImageRight != null) statusImageRight.gameObject.SetActive(false);
            }
        }

        // ★★★ 画像を一定時間後に非表示にするコルーチン ★★★
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
