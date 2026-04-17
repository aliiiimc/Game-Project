// Serializable hex grid coordinate system using axial coordinates (q-column and r-row axes) for board tile positioning.
using System;
using UnityEngine;

[Serializable]
public struct AxialCoord : IEquatable<AxialCoord>
{
    public int q;
    public int r;

    public AxialCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public bool Equals(AxialCoord other)
    {
        return q == other.q && r == other.r;
    }


    public override int GetHashCode()
    {
        unchecked
        {
            return (q * 397) ^ r;
        }
    }

    public override string ToString()
    {
        return $"({q}, {r})";
    }
}
