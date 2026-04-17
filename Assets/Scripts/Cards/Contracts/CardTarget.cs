// Unified target payload carrying board coordinates, card references, player identifiers, and entity IDs for validators and effects.
using System;

[Serializable]
public struct CardTarget
{
    public CardTargetType type;

    // Board coordinate used for tile-based targets.
    public AxialCoord tile;

    // Runtime card target when a unit/manifested card is selected.
    public CardRuntimeState targetCard;

    // Player id for player-level targeting.
    public string targetPlayerId;

    // Utility field for fort/hero identifiers.
    public string targetEntityId;
}
