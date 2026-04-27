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
        public void AddCardToHand(CardRuntimeState runtimeCard)
        {
            if (runtimeCard == null)
            {
                Debug.LogError("Runtime card is missing.");
                return;
            }



            if (cardPrefab == null || handContainer == null)
            {
                Debug.LogError("HandUI is missing a prefab or container reference!");
                return;
            }

            GameObject newCardObj = Instantiate(cardPrefab, handContainer);
            CardUI cardUI = newCardObj.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.runtimeCard = runtimeCard;

                if (cardUI.cardNameText != null)
                {
                    cardUI.cardNameText.text = runtimeCard.SourceCard.DisplayName;
                }

                if (cardUI.costText != null)
                {
                    cardUI.costText.text = runtimeCard.SourceCard.cost.ToString();
                }


                _cardsInHand.Add(cardUI);
            }
        }

        /// <summary>
        /// Removes a card UI object when the exact visual instance is already known.
        /// Use this version when another UI system already has a direct reference to
        /// the CardUI component that must be destroyed.
        /// </summary>
        public void RemoveCardFromHand(CardUI cardToRemove)
        {
            if (_cardsInHand.Contains(cardToRemove))
            {
                _cardsInHand.Remove(cardToRemove);
                Destroy(cardToRemove.gameObject);
            }
        }

        // A lire :

        /// <summary>
        /// Removes a card UI object by using the logical runtime card it represents.
        /// Each CardUI stores a reference to its CardRuntimeState in runtimeCard,
        /// so this method finds the matching visual card and destroys it.
        /// Use this version when game logic knows the runtime card to remove,
        /// but does not directly hold the CardUI reference.
        /// </summary>
        public void RemoveCardFromHand(CardRuntimeState runtimeCard) // surcharge dial lfonction li 9bl 
        {
            if (runtimeCard == null)
            {
                return;
            }

            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                CardUI cardUI = _cardsInHand[i];

                if (cardUI != null && cardUI.runtimeCard == runtimeCard)
                {
                    _cardsInHand.RemoveAt(i);
                    Destroy(cardUI.gameObject);
                    return;
                }
            }
        }

        public void ClearVisualSelection() //Quand on fais un discard forcé, le joueur peut cliquer sur une carte, puis changer d’avis et cliquer une autre. 
        // Sans nettoyage, plusieurs cartes peuvent rester jaunes (selectionées) à l’écran.
        // Cette fonction dit simplement: “avant de marquer une nouvelle carte comme sélectionnée, enlève d’abord la sélection visuelle sur toutes”.
        {
            for (int i = 0; i < _cardsInHand.Count; i++)
            {
                CardUI cardUI = _cardsInHand[i];

                if (cardUI != null)
                {
                    cardUI.SetSelected(false);  //appel de la fct 
                }
            }
        }

        /// <summary>
        /// Clears all cards (e.g. at the end of a match).
        /// </summary>
        public void ClearHand()
        {
            foreach (var card in _cardsInHand)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _cardsInHand.Clear();
        }
    }
}
