using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

public class NeutronStatistics : MonoBehaviour
{
    private static readonly string[] SizeSuffixes = { "B/s", "kB/s", "mB/s" };
    public static int clientBytesSent, clientBytesRec, serverBytesSent, serverBytesRec;
    [SerializeField] private float perSeconds = 1;

    [Header("Client Statistics")]
    [SerializeField] [ReadOnly] private string _BytesSent;
    [SerializeField] [ReadOnly] private string _BytesRec;

    [Header("Server Statistics")]
    [SerializeField] [ReadOnly] private string BytesSent;
    [SerializeField] [ReadOnly] private string BytesRec;

    private void Awake()
    {
        StartCoroutine(UpdateStatistics());
    }

    private void Start()
    {
        StartCoroutine(ClearStatistics());
    }

    private IEnumerator UpdateStatistics()
    {
        while (true)
        {
            _BytesSent = $"{SizeSuffix(clientBytesSent)}";
            _BytesRec = $"{SizeSuffix(clientBytesRec)}";
            yield return new WaitForSeconds(perSeconds);
        }
    }

    private IEnumerator ClearStatistics()
    {
        while (true)
        {
            clientBytesSent = 0;
            clientBytesRec = 0;
            yield return new WaitForSeconds(perSeconds);
        }
    }
    private static string SizeSuffix(int value, int decimalPlaces = 0)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} B/s", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

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
}