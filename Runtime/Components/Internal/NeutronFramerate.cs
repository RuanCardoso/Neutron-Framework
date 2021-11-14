using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_DISPATCHER)]
    public class NeutronFramerate : MonoBehaviour
    {
        #region Fields -> Inspector
#pragma warning disable IDE0044
        [SerializeField] private bool _drawOnGui = true;
        [SerializeField] private int _updateRate = 1;
        [SerializeField] [ValidateInput("IsHigh", "High precision Fps limit, but... High CPU usage!", 2)] FramerateLimitType _framerateLimitType = FramerateLimitType.Medium;
#pragma warning restore IDE0044
        #endregion

        #region Fields
        private int _frameCount = 0;
        private float _deltaTime = 0f;
        private float _currentFrameTime;
        private readonly YieldInstruction _waitForEndOfFrame = new WaitForEndOfFrame();
        #endregion

        #region Properties
        public static float Fps
        {
            get;
            private set;
        }
        public static float Ms
        {
            get;
            private set;
        }
        #endregion

#pragma warning disable IDE0051
        private void Start()
        {
            if (_framerateLimitType != FramerateLimitType.None)
                SetRateFrequency();
            switch (_framerateLimitType)
            {
                case FramerateLimitType.Low:
                    StartCoroutine(WaitForNextFrameLow(NeutronModule.Settings.GlobalSettings.Fps));
                    break;
                case FramerateLimitType.Medium:
                    Application.targetFrameRate = NeutronModule.Settings.GlobalSettings.Fps;
                    break;
                case FramerateLimitType.High:
                    StartCoroutine(WaitForNextFrameHigh(NeutronModule.Settings.GlobalSettings.Fps));
                    break;
            }
        }

        private void Update()
        {
            _deltaTime += Time.deltaTime;
            _frameCount++;

            if (_deltaTime > 1f / _updateRate)
            {
                Fps = Mathf.Round(_frameCount / _deltaTime);
                Ms = Mathf.Round(_deltaTime / _frameCount * 1000f);

                _deltaTime = 0f;
                _frameCount = 0;
            }
        }

        private void OnGUI()
        {
            if (_drawOnGui)
            {
                int padding = 1, height = 22, width = 100;
                GUI.Box(new Rect(Screen.width - width - padding, padding, width, height), $"Fps: {Fps}");
                GUI.Box(new Rect(Screen.width - width - padding, height + 5 + padding, width, height), $"Ms: {Ms}");
            }
        }

        private bool IsHigh() => _framerateLimitType != FramerateLimitType.High;
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

        enum FramerateLimitType
        {
            Low,
            Medium,
            High,
            None
        }
    }
}