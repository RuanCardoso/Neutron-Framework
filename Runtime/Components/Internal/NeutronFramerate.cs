using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronFramerate : MonoBehaviour
    {
        #region Fields
        [SerializeField] private int _updateRate = 4;
        private int _frameCount = 0;
        private float _deltaTime = 0f;
        #endregion

        #region Properties
        public static float Fps {
            get;
            private set;
        }
        public static float Ms {
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
    }
}