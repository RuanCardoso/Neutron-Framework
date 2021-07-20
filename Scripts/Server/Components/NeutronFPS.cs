using NeutronNetwork;
using UnityEngine;
using UnityEngine.UI;

public class NeutronFPS : MonoBehaviour
{
    [SerializeField] private int m_UpdateRate = 4;
    [SerializeField] private Text m_FpsText;

    #region Variables
    private int m_FrameCount = 0;
    private float m_DeltaTime = 0f;
    private float m_Fps = 0f;
    private float m_Ms = 0f;
    #endregion

    void Update()
    {
        m_DeltaTime += Time.unscaledDeltaTime;
        m_FrameCount++;

        if (m_DeltaTime > 1f / m_UpdateRate)
        {
            m_Fps = m_FrameCount / m_DeltaTime;
            m_Ms = m_DeltaTime / m_FrameCount * 1000f;

            string Stats = string.Format("{0} FPS / {1:0.0} Ms", Mathf.RoundToInt(m_Fps), m_Ms);

#if !UNITY_SERVER
            m_FpsText.text = Stats;
#else
            //NeutronLogger.Print(Stats);
#endif

            #region Reset
            m_DeltaTime = 0f;
            m_FrameCount = 0;
            #endregion
        }
    }
}