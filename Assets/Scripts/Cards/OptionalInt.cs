// Serializable nullable-int replacement enabling optional integer fields in Unity Inspector for cases where a value may be absent.
using System;
using UnityEngine;

[Serializable]
public struct OptionalInt
{
    // True when this wrapper currently stores a number.
    [SerializeField] private bool hasValue;

    // Stored integer value when hasValue is true.
    [SerializeField] private int value;

    public bool HasValue => hasValue;
    public int Value => value;

    // Creates an OptionalInt that contains a value.
    public OptionalInt(int value)
    {
        hasValue = true;
        this.value = value;
    }

    // Convenience factory for "no value".
    public static OptionalInt None => new OptionalInt();

    // Useful for debug logs and inspector tooling.
    public override string ToString()
    {
        return hasValue ? value.ToString() : "None";
    }
}
