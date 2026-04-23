using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardLibrary", menuName = "Cards/Card Library")]
public class CardLibrary : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();

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
