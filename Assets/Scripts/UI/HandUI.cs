using System.Collections.Generic;
using UnityEngine;

namespace FortGame.UI 
{
    /// <summary>
    /// Manages the collection of CardUI objects currently in the player's hand.
    /// Typically attached to a UI Panel that has a HorizontalLayoutGroup component.
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject cardPrefab; // The visual template for a card
        public Transform handContainer; // The LayoutGroup transform that holds the cards

        private List<CardUI> _cardsInHand = new List<CardUI>();

        /// <summary>
        /// Instantiates a new card visually in the hand.
        /// </summary>
        public void AddCardToHand(string cardName, int cost)
        {
            if (cardPrefab == null || handContainer == null)
            {
                Debug.LogError("HandUI is missing a prefab or container reference!");
                return;
            }

            GameObject newCardObj = Instantiate(cardPrefab, handContainer);
            CardUI cardUI = newCardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                if (cardUI.cardNameText != null) cardUI.cardNameText.text = cardName;
                if (cardUI.costText != null) cardUI.costText.text = cost.ToString();
                
                _cardsInHand.Add(cardUI);
            }
        }

        /// <summary>
        /// Removes a specific card from the hand visually.
        /// </summary>
        public void RemoveCardFromHand(CardUI cardToRemove)
        {
            if (_cardsInHand.Contains(cardToRemove))
            {
                _cardsInHand.Remove(cardToRemove);
                Destroy(cardToRemove.gameObject);
            }
        }

        /// <summary>
        /// Clears all cards (e.g. at the end of a match).
        /// </summary>
        public void ClearHand()
        {
            foreach(var card in _cardsInHand)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _cardsInHand.Clear();
        }
    }
}
