using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronFramerate : MonoBehaviour
    {
        #region Fields -> Inspector
        [SerializeField] private bool _drawOnGui = true;
        [SerializeField] private int _updateRate = 4;
        #endregion

        #region Fields
        private int _frameCount = 0;
        private float _deltaTime = 0f;
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

        private void Update()
        {
            _deltaTime += Time.unscaledDeltaTime;
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
                int padding = 1, height = 20, width = 100;
                GUI.Box(new Rect(Screen.width - width - padding, padding, width, height), $"Fps: {Fps}");
                GUI.Box(new Rect(Screen.width - width - padding, height + 5 + padding, width, height), $"Ms: {Ms}");
            }
        }
    }
}