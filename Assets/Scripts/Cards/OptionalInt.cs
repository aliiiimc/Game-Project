using System;
using UnityEngine;

[Serializable]
public struct OptionalInt
{
    [SerializeField] private bool hasValue;

    [SerializeField] private int value;

    public bool HasValue => hasValue;
    public int Value => value;

    public OptionalInt(int value)
    {
        hasValue = true;
        this.value = value;
    }

    public static OptionalInt None => new OptionalInt();

    public override string ToString()
    {
        return hasValue ? value.ToString() : "None";
    }
}
