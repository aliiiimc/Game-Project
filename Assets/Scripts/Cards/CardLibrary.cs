// Centralized registry of available card definitions accessible by display name; acts as the master card library for game modes.
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardLibrary", menuName = "Cards/Card Library")]
public class CardLibrary : ScriptableObject
{
    // Master list of card definitions available to this game mode.
    public List<CardData> cards = new List<CardData>();

    // Finds a card by UI display name.
    public CardData GetByDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            if (card != null && card.DisplayName == displayName)
            {
                return card;
            }
        }

        return null;
    }
}
