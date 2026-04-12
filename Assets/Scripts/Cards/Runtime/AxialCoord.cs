using System;
using UnityEngine;

// Axial coordinate for hex grids: q (column axis) and r (row axis).
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

    public override bool Equals(object obj)
    {
        return obj is AxialCoord other && Equals(other);
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
