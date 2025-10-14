using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System;

namespace Mitsunoazi
{
    public class IdleVideoController : MonoBehaviour
    {
        [Serializable]
        public class VideoPlaybackUI
        {
            public VideoPlayer player;
            public RawImage displayImage;
        }

        [Header("Playback UIs")]
        [SerializeField]
        private VideoPlaybackUI leftUI;

        [SerializeField]
        private VideoPlaybackUI rightUI;

        [Header("Settings")]
        [SerializeField]
        private float idleDelaySeconds = 5.0f;

        private Coroutine leftIdleCoroutine;
        private Coroutine rightIdleCoroutine;
        private bool isLeftVideoPlaying = false;
        private bool isRightVideoPlaying = false;

        private void Start()
        {
            InitializeUI(leftUI);
            InitializeUI(rightUI);
        }

        private void InitializeUI(VideoPlaybackUI ui)
        {
            if (ui.displayImage != null)
            {
                ui.displayImage.color = Color.clear;
            }
            if (ui.player != null)
            {
                ui.player.playOnAwake = false;
                ui.player.Stop();
            }
        }

        private void Update()
        {
            if (Input.anyKeyDown)
            {
                if (isLeftVideoPlaying)
                {
                    StopIdleVideo(leftUI, () => isLeftVideoPlaying = false);
                    if (leftIdleCoroutine != null) StopCoroutine(leftIdleCoroutine);
                }
                if (isRightVideoPlaying)
                {
                    StopIdleVideo(rightUI, () => isRightVideoPlaying = false);
                    if (rightIdleCoroutine != null) StopCoroutine(rightIdleCoroutine);
                }
            }
        }

        public void ResetIdleTimer(int cameraIndex)
        {
            bool isEven = cameraIndex % 2 == 0;

            if (isEven)
            {
                if (leftIdleCoroutine != null) StopCoroutine(leftIdleCoroutine);
                if (isLeftVideoPlaying) StopIdleVideo(leftUI, () => isLeftVideoPlaying = false);
                leftIdleCoroutine = StartCoroutine(IdleCheckRoutine(leftUI, () => isLeftVideoPlaying = true));
            }
            else
            {
                if (rightIdleCoroutine != null) StopCoroutine(rightIdleCoroutine);
                if (isRightVideoPlaying) StopIdleVideo(rightUI, () => isRightVideoPlaying = false);
                rightIdleCoroutine = StartCoroutine(IdleCheckRoutine(rightUI, () => isRightVideoPlaying = true));
            }
        }

        public void CancelIdleState(int cameraIndex)
        {
            bool isEven = cameraIndex % 2 == 0;
            if (isEven)
            {
                if (leftIdleCoroutine != null) StopCoroutine(leftIdleCoroutine);
                if (isLeftVideoPlaying) StopIdleVideo(leftUI, () => isLeftVideoPlaying = false);
            }
            else
            {
                if (rightIdleCoroutine != null) StopCoroutine(rightIdleCoroutine);
                if (isRightVideoPlaying) StopIdleVideo(rightUI, () => isRightVideoPlaying = false);
            }
        }

        private IEnumerator IdleCheckRoutine(VideoPlaybackUI ui, Action onPlayStart)
        {
            yield return new WaitForSeconds(idleDelaySeconds);
            PlayIdleVideo(ui, onPlayStart);
        }

        private void PlayIdleVideo(VideoPlaybackUI ui, Action onPlayStart)
        {
            if (ui.player != null && ui.displayImage != null)
            {
                ui.displayImage.color = Color.white;
                ui.player.isLooping = true; // ループを有効化
                ui.player.Play();

                string side = (ui == leftUI) ? "Left" : "Right";
                Debug.Log($"[IdleVideoController] {side} side video playback started.");

                onPlayStart?.Invoke();
            }
        }

        private void StopIdleVideo(VideoPlaybackUI ui, Action onPlayStop)
        {
            if (ui.player != null && ui.displayImage != null)
            {
                ui.player.Stop();
                ui.player.isLooping = false; // ループを無効化
                ui.displayImage.color = Color.clear;
                onPlayStop?.Invoke();
            }
        }
    }
}