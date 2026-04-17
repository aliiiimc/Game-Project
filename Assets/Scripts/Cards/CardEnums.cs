// // Rarity tier used for drop tables, shop rates, and balance.
// public enum CardRarity
// {
//     Common,
//     Rare,
//     Legendary
// }

// Enumerations defining spell effect types (Buff, Damage, Heal, etc.) and world effect categories (Structure, Hazard, Weather, etc.).
public enum SpellEffectType
{
    Buff,
    Debuff,
    Damage,
    Boost,
    Heal,
    Summon,
    Utility
}

// Subtype used by world-effect cards (structures, fields, hazards, weather).
public enum WorldEffectCategory
{
    Structure,
    ResourceField,
    Hazard,
    Weather,
    ZoneEffect
}

// Runtime location of a card during the match lifecycle.
public enum CardZone
{
    Deck,
    Hand,
    Board,
    Discard,
    // Exile
}
