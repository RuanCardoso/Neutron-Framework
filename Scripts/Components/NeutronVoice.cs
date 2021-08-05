using System;
using System.Collections.Generic;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Naughty.Attributes;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Neutron/Neutron Voice Chat")]
    public class NeutronVoice : NeutronBehaviour
    {
        #region Singleton
        public static NeutronVoice instance;
        #endregion

        #region Static
        public static bool enableMobileMicInput;
        #endregion
        [ReadOnly] public string deviceName;

        [Header("[Mic Settings]")]
        [SerializeField] private KeyCode keyCode = KeyCode.T;
        [SerializeField] [Range(0, 300)] private int lengthSec = 60;
        [SerializeField] [Range(0, 10)] private int stopDelay = 2;
        [SerializeField] private int Gain = 1;
        [SerializeField] private int Frequency = 8000;
        [SerializeField] [Range(0, 5)] private float samplesTime = 1f;
        [SerializeField] private bool Playback = false;
        [SerializeField] private bool realtimeSamples = false;

        [Header("[Component]")]
        [ReadOnly] public AudioSource audioSource;

        [Header("[General Settings]")]
        [SerializeField] [Range(0, 5)] private float synchronizeInterval = 1f;
        [SerializeField] private TargetTo sendTo = TargetTo.Others;
        [SerializeField] private TunnelingTo broadcast = TunnelingTo.Room;
        [SerializeField] private Protocol protocol = Protocol.Udp;
        [ReadOnly] public string[] devicesName;
        private AudioClip audioClip;
        private int offset;
        private float tSyncInterval, tSamplesTime, tStopDelay = 100;

        private new void Awake()
        {
            base.Awake();
            instance = this;
        }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
        }

        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            SetIntervals();
            if (HasAuthority)
                Init();
        }

        public void Init()
        {
            if (!Microphone.IsRecording(deviceName))
            {
                ResetOffset();
                Microphone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
                if (Frequency == 0) Frequency = minFreq;
                else if (Frequency == 1) Frequency = maxFreq;
                audioClip = Microphone.Start(deviceName, false, lengthSec, Frequency);
                if (audioClip != null)
                    SettingUp();
            }
            else BroadcastAudio();
        }

        public void Stop()
        {
            if (Microphone.IsRecording(deviceName))
                Microphone.End(deviceName);
            ResetOffset();
        }

        private void RealtimePlayback()
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            if (realtimeSamples) audioSource.timeSamples = Microphone.GetPosition(deviceName);
        }

        private void BroadcastAudio()
        {
            bool GetData(ref float[] samples)
            {
                return audioClip.GetData(samples, offset);
            }

            void ResetTimeAndSetOffset(int rPos)
            {
                tSamplesTime = 0;
                offset = rPos;
            }

            void Broadcast(float[] samples)
            {
                void ResetStopDelay()
                {
                    tStopDelay = 0;
                }

                void Send()
                {
                    if (tSyncInterval >= synchronizeInterval)
                    {
                        using (var options = Neutron.PooledNetworkWriters.Pull())
                        {
                            options.Write(Frequency);
                            options.Write(audioClip.channels);
                            options.Write(samples);
                            //iRPC(10021, options, Cache.Overwrite, sendTo, broadcast, protocol);
                        }
                        tSyncInterval = 0;
                    }
                }

#if UNITY_STANDALONE
                if (Input.GetKey(keyCode))
#else
                if (enableMobileMicInput)
#endif
                {
                    ResetStopDelay();
                    Send();
                }
                else if (!(tStopDelay >= stopDelay))
                    Send();
            }

            int pos;
            if ((pos = Microphone.GetPosition(null)) > 0)
            {
                int diff = pos - offset;
                if (diff > 0)
                {
                    float[] samples = new float[diff * audioClip.channels];
                    if (GetData(ref samples))
                    {
                        if (tSamplesTime >= samplesTime)
                        {
                            Broadcast(samples);
                            ResetTimeAndSetOffset(pos);
                        }
                    }
                }
            }
        }

        private void DecodeToAudio(float[] data, int channels, int freq)
        {
            void Increase()
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = data[i] * Gain;
                }
            }
            if (Gain > 1) Increase();
            var Clip = AudioClip.Create("VoiceChat", data.Length, channels, freq, false);
            if (Clip.SetData(data, 0))
            {
                audioSource.clip = Clip;
                if (!audioSource.isPlaying) audioSource.Play();
            }
        }

        private void ResetOffset()
        {
            offset = 0;
        }

        private void SetIntervals()
        {
            float t = Time.deltaTime;
            tSyncInterval += t;
            tSamplesTime += t;
            tStopDelay += t;
        }

        private void SettingUp()
        {
            devicesName = Microphone.devices;
            if (Playback)
                RealtimePlayback();
            while (!(Microphone.GetPosition(deviceName) > 0)) { }
        }

        [iRPC(ID = 10)]
        private void RPC(NeutronReader options, NeutronPlayer sender)
        {
            Debug.Log(IsClient);
            using (options)
            {
                if (IsClient)
                {
                    int freq = options.ReadInt32();
                    int channels = options.ReadInt32();
                    float[] data = options.ReadFloatArray();
                    DecodeToAudio(data, channels, freq);
                }
            }
        }
    }
}