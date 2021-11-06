using NeutronNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DERIK : SyncVarBehaviour
{
    [SyncVar(nameof(OnPointsChanged))] public int _points;
    [SyncVar(nameof(OnKillsChanged))] public float _kils;
    [SyncVar(nameof(OnListChanged))] public List<int> listOfInts;
    [SyncVar(nameof(OnPersonChanged))] public Person person;
    [SyncVar(nameof(OnLongTimeChanged))] public double time;
    [SyncVar(nameof(OnDoubleTimeChanged))] public long longTime;

    private void OnPointsChanged(int newValue)
    {
        LogHelper.Error("int: " + newValue + " old: " + _points);
    }

    private void OnKillsChanged(float value)
    {
        LogHelper.Error("float: " + value);
    }

    private void OnListChanged(List<int> newList)
    {

    }

    private void OnPersonChanged(Person newPerson)
    {
        LogHelper.Error("pessoa mudou");
    }

    private void OnLongTimeChanged(long newTime)
    {

    }

    private void OnDoubleTimeChanged(double newTime)
    {

    }
}

[Serializable]
public class Person
{
    [SyncVar] public string name;
}

//public class DicInt : SerializedMonoBehaviour