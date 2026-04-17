using UnityEngine;
using UnityEngine.Serialization;

// Base data model shared by all card types (Character, Spell, WorldEffect).
public abstract class CardData : ScriptableObject
{
    [Header("Core")]
    // Display name shown in UI; falls back to asset name if empty.
    public string cardName;

    // Money price required to play this card.
    public int cost;

    // Sprite used when card is shown as a card object (hand, deck, shop, previews).
    [FormerlySerializedAs("artwork")]
    public Sprite handDeckSprite;


    // public CardRarity rarity = CardRarity.Common;

    // Rules text shown to the player.
    [TextArea]
    public string description;

    // Child classes provide type-specific movement fields through this property.
    public abstract OptionalInt MovementCapacity { get; }

    // Safe card title used by lookup code and UI.
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;

    // Creates a mutable runtime snapshot used during the match.
    public virtual CardRuntimeState CreateRuntimeState()
    {
        return new CardRuntimeState(this);
    }
}
