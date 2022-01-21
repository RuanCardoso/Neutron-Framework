using MarkupAttributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronFramerate : MarkupBehaviour
    {
        #region Fields -> Inspector
#pragma warning disable IDE0044
        [InfoBox("This can have some impact on the GC.", EInfoBoxType.Warning)]
        [SerializeField] [Box("Framerate Options")] private bool _drawOnGui = true; // Draws the framerate on the screen.
        [SerializeField] [Range(1, 10)] private int _updateRate = 1; // The update rate of the framerate.
        [InfoBox("This can have some impact on the CPU.", EInfoBoxType.Warning)]
        [SerializeField] FramerateLimitType _frameratePrecision = FramerateLimitType.Medium; // The precision of the framerate.
        [SerializeField] [Range(1, NeutronConstants.MAX_FPS)] private int _fps = 60; // The target framerate.

        [SerializeField] [MarkupAttributes.ShowIf(nameof(_drawOnGui))] [Box("GUI Options")] private int _padding = 1; // The padding of the GUI.
        [SerializeField] [MarkupAttributes.ShowIf(nameof(_drawOnGui))] private int _height = 22; // The padding of the GUI.
        [SerializeField] [MarkupAttributes.ShowIf(nameof(_drawOnGui))] private int _width = 100; // The padding of the GUI.
#pragma warning restore IDE0044
        #endregion

        #region Fields
        private int _frameCount = 0; // The frame count.
        private float _deltaTime = 0f; // The delta time.
        private float _currentFrameTime; // The current frame time.
        private readonly YieldInstruction _waitForEndOfFrame = new WaitForEndOfFrame(); // The wait for end of frame.
        private readonly YieldInstruction _waitForEndOfSeconds = new WaitForSeconds(1); // The wait for end of frame.
        #endregion

        #region Properties
        /// <summary>
        /// The currrent Fps.
        /// </summary>
        /// <value></value>
        public static float Fps
        {
            get;
            private set;
        }

        /// <summary>
        /// The current Ms(Cpu).
        /// </summary>
        /// <value></value>
        public static float Ms
        {
            get;
            private set;
        }
        #endregion

#pragma warning disable IDE0051
        private void Start()
        {
            int fps = _fps; // Get the fps from the settings.
            if (_frameratePrecision != FramerateLimitType.None)
                SetRateFrequency(); // Set the rate frequency.

            switch (_frameratePrecision)
            {
                case FramerateLimitType.Low:
                    StartCoroutine(WaitForNextFrameLow(fps)); // Start the coroutine.
                    break;
                case FramerateLimitType.Medium:
                    StartCoroutine(WaitForNextFrameMedium(fps)); // Start the coroutine.
                    break;
                case FramerateLimitType.High:
                    StartCoroutine(WaitForNextFrameHigh(fps)); // Start the coroutine.
                    break;
            }

            useGUILayout = _drawOnGui; // Enable or disable the use of the GUI.
        }

        private void Update()
        {
            _deltaTime += Time.deltaTime; // Add the delta time.
            _frameCount++; // Add the frame count.

            if (_deltaTime > 1f / _updateRate)
            {
                // Update the fps.

                Fps = Mathf.Round(_frameCount / _deltaTime);
                Ms = Mathf.Round(_deltaTime / _frameCount * 1000f);

                _deltaTime = 0f;
                _frameCount = 0;
            }
        }

        GUIStyle _style;
        private void OnGUI()
        {
            // Draw the framerate on the screen.
            if (_drawOnGui)
            {
                _style = new GUIStyle(GUI.skin.box);

                // Calculate font size based on height and width.
                _style.fontSize = Mathf.RoundToInt(Mathf.Min(_height, _width) / 1.5f);

                int padding = _padding, height = _height, width = _width; // Get the padding and height.
                GUI.Box(new Rect(Screen.width - width - padding, padding, width, height), $"Fps: {Fps}", _style); // Draw the box.
                GUI.Box(new Rect(Screen.width - width - padding, height + 5 + padding, width, height), $"Ms: {Ms}", _style); // Draw the box.

                _style = null; // Reset the style.
            }
        }
#pragma warning restore IDE0051

        private void SetRateFrequency()
        {
            QualitySettings.vSyncCount = 0;
            _currentFrameTime = Time.realtimeSinceStartup;
        }

        private IEnumerator WaitForNextFrameHigh(float rate)
        {
            while (true)
            {
                yield return _waitForEndOfFrame;
                _currentFrameTime += 1.0F / rate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = _currentFrameTime - t - 0.01f;
                if (sleepTime > 0)
                    Thread.Sleep((int)(sleepTime * 1000));
                while (t < _currentFrameTime)
                    t = Time.realtimeSinceStartup;
            }
        }

        private IEnumerator WaitForNextFrameLow(float rate)
        {
            while (true)
            {
                yield return _waitForEndOfFrame;
                _currentFrameTime += 1.0F / rate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = _currentFrameTime - t - 0.01f;
                if (sleepTime > 0)
                    Thread.Sleep((int)(sleepTime * 1000));
                if (t < _currentFrameTime)
                    t = Time.realtimeSinceStartup;
            }
        }

        private IEnumerator WaitForNextFrameMedium(int rate)
        {
            while (true)
            {
                Application.targetFrameRate = rate;
                yield return _waitForEndOfSeconds;
            }
        }

        enum FramerateLimitType
        {
            Low,
            Medium,
            High,
            None
        }
    }
}