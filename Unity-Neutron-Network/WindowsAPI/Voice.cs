//using UnityEngine;
//using VoiceAPI;

//public class Voice : NeutronBehaviour
//{
//    private string path;
//    [SerializeField] private bool isEnable = true;
//    [SerializeField] private int Frequency = 8000;
//    [SerializeField] private int bitRate = 16;
//    [SerializeField] private int KeyCode = 84;
//    public override void OnConnected(bool success)
//    {
//        if (success)
//        {
//            if (isEnable)
//            {
//#if UNITY_STANDALONE_WIN
//                path = $"{Application.dataPath}\\VNeutron\\VNeutron.exe";
//                if (!WinAPI.InitWindowsSoundAPI(path, NeutronCConst._IEPListen.Port, NeutronCConst._IEPSend.Address.ToString(), bitRate, Frequency, KeyCode))
//                {
//                    Utils.LoggerError("SDK Sound Win API Not Found!", true);
//                }
//#else
//Utils.LoggerError("SDK Sound is not compatible with this System");
//#endif
//            }
//        }
//    }
//}