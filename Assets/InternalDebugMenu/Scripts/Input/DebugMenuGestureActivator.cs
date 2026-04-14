using UnityEngine;

namespace InternalDebugMenu
{
    /// <summary>
    /// Hidden in-game activation gesture. Uses only the game's input loop.
    /// </summary>
    public sealed class DebugMenuGestureActivator : MonoBehaviour
    {
        [SerializeField] private DebugManager debugManager;
        [SerializeField] private bool allowEditorShortcut = true;
        [SerializeField] private KeyCode editorShortcut = KeyCode.F10;
        [SerializeField] [Min(4.0f)] private float stationaryThresholdPixels = 32.0f;

        private float holdStartTime = -1.0f;
        private float cooldownEndsAt;
        private bool gestureConsumed;

        private void Awake()
        {
            if (debugManager == null)
            {
                debugManager = GetComponent<DebugManager>();
            }
        }

        private void Update()
        {
            if (debugManager == null || !debugManager.IsSupportedBuild)
            {
                return;
            }

#if UNITY_EDITOR
            if (allowEditorShortcut && Input.GetKeyDown(editorShortcut))
            {
                debugManager.TryToggleMenu();
            }
#endif

            HandleThreeFingerHold();
        }

        private void HandleThreeFingerHold()
        {
            var settings = debugManager.Settings;
            if (Time.unscaledTime < cooldownEndsAt || settings == null)
            {
                return;
            }

            if (Input.touchCount < 3)
            {
                ResetGesture();
                return;
            }

            var thresholdSquared = stationaryThresholdPixels * stationaryThresholdPixels;

            for (var index = 0; index < 3; index++)
            {
                var touch = Input.GetTouch(index);

                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    ResetGesture();
                    return;
                }

                if (touch.deltaPosition.sqrMagnitude > thresholdSquared)
                {
                    ResetGesture();
                    return;
                }
            }

            if (holdStartTime < 0.0f)
            {
                holdStartTime = Time.unscaledTime;
            }

            if (gestureConsumed)
            {
                return;
            }

            if (Time.unscaledTime - holdStartTime < settings.ThreeFingerHoldSeconds)
            {
                return;
            }

            if (debugManager.TryToggleMenu())
            {
                cooldownEndsAt = Time.unscaledTime + settings.GestureCooldownSeconds;
            }

            gestureConsumed = true;
        }

        private void ResetGesture()
        {
            holdStartTime = -1.0f;
            gestureConsumed = false;
        }
    }
}
