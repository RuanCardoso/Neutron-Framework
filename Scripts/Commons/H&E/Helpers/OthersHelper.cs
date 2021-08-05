using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Server.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public static class OthersHelper
    {
        #region Fields
        private static readonly string[] SizeSuffixes = { "B/s", "kB/s", "mB/s", "gB/s" };
        private static int[] ClassifiedOdds;
        private static readonly System.Random Rnd = new System.Random();
        #endregion

        #region Collections
        private static List<int> Numbers = new List<int>();
        #endregion

        public static string SizeSuffix(long value, int mag = 0, int decimalPlaces = 2) // From StackOverflow
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} B/s", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            if (mag <= 0)
                mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        public static void Odds(int percent)
        {
            if (percent > 100)
                percent = 100;

            // Inicializa a Matriz
            ClassifiedOdds = new int[percent];
            //Limpa a lista.
            Numbers.Clear();
            // Adicionar 100 números para fazer a jogada de 0% a 100%.
            for (int i = 1; i <= 100; i++)
                Numbers.Add(i);
            // Bagunça a lista.
            Numbers = new List<int>(Numbers.OrderBy(x => Rnd.Next(1, 100)));
            // Faz o sorteio de sorte/porcetagem.
            for (int i = 0; i < ClassifiedOdds.Length; i++)
                ClassifiedOdds[i] = Numbers[i];
        }

        public static bool Odds()
        {
            if (ClassifiedOdds == null)
                return LogHelper.Error("ClassifiedOdds it cannot be null.");
            return
                !ClassifiedOdds.Contains(Rnd.Next(1, 100));
        }

        public static Packet ReadPacket(byte[] packetBuffer)
        {
            using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
            {
                reader.SetBuffer(packetBuffer);
                return reader.ReadPacket<Packet>();
            }
        }

        public static void SetColor(NeutronView neutronView, Color color)
        {
            Renderer renderer = neutronView.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        public static NeutronDefaultHandlerSettings GetDefaultHandler()
        {
            return NeutronModule.Synchronization.DefaultHandlers;
        }

        public static Settings GetSettings()
        {
            return NeutronModule.Settings;
        }

        public static NeutronConstantsSettings GetConstants()
        {
            return NeutronModule.Settings.NetworkSettings;
        }

#if !UNITY_2019_2_OR_NEWER
        public static bool TryGetComponent<T>(this UnityEngine.GameObject monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }

        public static bool TryGetComponent<T>(this UnityEngine.Transform monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }
#endif
    }
}