using System;

[Serializable]
public struct CardTarget
{
    public CardTargetType type;

    public AxialCoord tile;

    public CardRuntimeState targetCard;

    public string targetPlayerId;

    public string targetEntityId;
}
