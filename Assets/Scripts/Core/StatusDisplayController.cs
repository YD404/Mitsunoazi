// Assets/Scripts/Sys/StatusDisplayController.cs

// 必要な名前空間をインポートします
using UnityEngine;
using UnityEngine.UI; // ImageなどのUIコンポーネントを扱うために必要
using System;         // Serializable属性（インスペクター表示用）を扱うために必要
using System.Collections; // コルーチン（時間差処理）を扱うために必要

namespace Mitsunoazi
{
    /// <summary>
    /// ステータスの変更に応じて、UIの画像表示と音声再生を制御するクラス
    /// </summary>
    public class StatusDisplayController : MonoBehaviour
    {
        // =====================================================================
        // フィールド変数（インスペクターから設定するもの）
        // =====================================================================

        [Header("Required Components")] // インスペクター上で見やすくするためのヘッダー
        [Tooltip("イベントの発生元となるCaptureStateManager")] // インスペクターでマウスオーバーした際に表示される説明文
        [SerializeField]
        private CaptureStateManager captureStateManager;
        
        [Tooltip("効果音を再生するためのAudioSourceコンポーネント")]
        [SerializeField]
        private AudioSource audioSource;

        [Tooltip("アイドル動画再生を制御するコントローラー")]
        [SerializeField]
        private IdleVideoController idleVideoController;

        [Header("UI Elements")]
        [Tooltip("カメラIndexが偶数の時に使用するImage")]
        [SerializeField]
        private Image statusImageLeft;

        [Tooltip("カメラIndexが奇数の時に使用するImage")]
        [SerializeField]
        private Image statusImageRight;

        [Header("Settings")]
        [Tooltip("UIが自動で非表示になるまでの時間（秒）")]
        [SerializeField]
        private float displayTimeoutSeconds = 5.0f;

        [Header("Status Assets")]
        [Tooltip("ステータス変更時に再生する共通の効果音")]
        [SerializeField]
        private AudioClip commonStatusChangeClip;

        [Tooltip("ステータス確定時に再生する共通の効果音")]
        [SerializeField]
        private AudioClip confirmationClip;

        [Tooltip("各ステータスと、それに対応するスプライト画像のセット")]
        [SerializeField]
        private StatusUIData[] statusUIDataArray; 

        // =====================================================================
        // プライベート変数（スクリプト内部でのみ使用）
        // =====================================================================

        // 実行中のタイムアウト処理（コルーチン）を管理するための変数。途中でキャンセルするのに使う。
        private Coroutine leftTimeoutCoroutine;  // 左側UIのタイマー
        private Coroutine rightTimeoutCoroutine; // 右側UIのタイマー

        /// <summary>
        /// インスペクター上でステータスとスプライトを紐付けるためのデータ構造
        /// </summary>
        [Serializable] // この属性を付けると、インスペクター上に表示・編集できるようになる
        public class StatusUIData
        {
            public StatusManager.Status status;
            public Sprite sprite;
        }

        // =====================================================================
        // MonoBehaviourのライフサイクルメソッド
        // =====================================================================

        /// <summary>
        /// ゲーム開始時に一度だけ呼ばれる処理
        /// </summary>
        private void Start()
        {
            // 念のため、起動時に両方の画像を非表示にしておく
            if (statusImageLeft != null) statusImageLeft.gameObject.SetActive(false);
            if (statusImageRight != null) statusImageRight.gameObject.SetActive(false);
        }

        /// <summary>
        /// このオブジェクトが有効になった時に呼ばれる処理
        /// </summary>
        private void OnEnable()
        {
            if (captureStateManager != null)
            {
                // CaptureStateManagerが持つイベントに対して、実行したい処理（メソッド）を登録（購読）する
                captureStateManager.OnStatusChanged += HandleStatusChange;
                captureStateManager.OnConfirm += HandleConfirm;
            }
        }

        /// <summary>
        /// このオブジェクトが無効になった時に呼ばれる処理
        /// </summary>
        private void OnDisable()
        {
            // OnEnableで登録したイベントを解除する。
            // これをしないと、オブジェクトが破棄された後も処理が呼ばれ続け、エラーやメモリリークの原因になる。
            if (captureStateManager != null)
            {
                captureStateManager.OnStatusChanged -= HandleStatusChange;
                captureStateManager.OnConfirm -= HandleConfirm;
            }
        }

        // =====================================================================
        // イベントハンドラ（イベント発生時に実行されるメソッド）
        // =====================================================================

        /// <summary>
        /// ステータスが変更された（OnStatusChanged）イベント発生時に実行される処理
        /// </summary>
        /// <param name="cameraIndex">変更があったカメラの番号</param>
        /// <param name="newStatus">変更後の新しいステータス</param>
        private void HandleStatusChange(int cameraIndex, StatusManager.Status newStatus)
        {
            // ステータス変更音を再生する
            if (audioSource != null && commonStatusChangeClip != null)
            {
                audioSource.PlayOneShot(commonStatusChangeClip);
            }

            // アイドル動画コントローラーにタイマーのリセットを指示する
            if (idleVideoController != null)
            {
                idleVideoController.ResetIdleTimer(cameraIndex);
            }

            // カメラ番号が偶数か奇数かで、操作対象のUI（とタイマー）を決定する
            bool isEven = cameraIndex % 2 == 0;
            Image targetImage = isEven ? statusImageLeft : statusImageRight;

            // 該当する側のタイマーをリセットし、新しいタイマーを開始する
            if (isEven)
            {
                // もし既にタイマーが動いていたら、それを停止する（タイマーのリセット）
                if (leftTimeoutCoroutine != null) StopCoroutine(leftTimeoutCoroutine);
                // 新しいタイマーを開始し、管理変数に保持する
                leftTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }
            else
            {
                if (rightTimeoutCoroutine != null) StopCoroutine(rightTimeoutCoroutine);
                rightTimeoutCoroutine = StartCoroutine(ImageTimeoutRoutine(targetImage));
            }
            
            // 新しいステータスに対応するスプライト画像を配列から探す
            Sprite targetSprite = null;
            foreach (var data in statusUIDataArray)
            {
                if (data.status == newStatus)
                {
                    targetSprite = data.sprite;
                    break; // 見つかったらループを抜ける
                }
            }

            // 対象のUI Imageに、見つけたスプライトを設定し、表示を有効化する
            if (targetImage != null)
            {
                targetImage.sprite = targetSprite;
                targetImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// ステータスが確定された（OnConfirm）イベント発生時に実行される処理
        /// </summary>
        /// <param name="cameraIndex">確定があったカメラの番号</param>
        private void HandleConfirm(int cameraIndex)
        {
            Debug.Log($"[StatusDisplayController] カメラ {cameraIndex} の確定イベント受信");

            // アイドル動画コントローラーに監視の完全停止を指示する
            if (idleVideoController != null)
            {
                idleVideoController.CancelIdleState(cameraIndex);
            }
            // 確定音を再生する
            if (audioSource != null && confirmationClip != null)
            {
                audioSource.PlayOneShot(confirmationClip);
                Debug.Log($"[StatusDisplayController] 確定音再生");
            }

            // ★★★ タイムライン再生中フラグをオンにする ★★★
            Director.IsTimelinePlaying = true;
            Debug.Log($"[StatusDisplayController] タイムライン再生フラグをONに設定");

            // 確定されたカメラが偶数か奇数かを判断し、該当する側のタイマー停止と非表示処理のみを行う
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

        // =====================================================================
        // コルーチン
        // =====================================================================

        /// <summary>
        /// 指定されたImageを一定時間後に非表示にする時間差処理（コルーチン）
        /// </summary>
        /// <param name="imageToHide">非表示にしたいImageコンポーネント</param>
        private IEnumerator ImageTimeoutRoutine(Image imageToHide)
        {
            // displayTimeoutSecondsで指定された秒数だけ、この場所で処理を待機する
            yield return new WaitForSeconds(displayTimeoutSeconds);

            // 待機後、対象のImageを非表示にする
            if (imageToHide != null)
            {
                imageToHide.gameObject.SetActive(false);
            }
        }
    }
}
