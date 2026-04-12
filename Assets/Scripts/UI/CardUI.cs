using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace FortGame.UI 
{
    /// <summary>
    /// Represents a single Card visually on the screen.
    /// Handles mouse interactions like hover, drag, and drop.
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Card Visuals")]
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI costText;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;

        // Variables to remember where the card belongs when dragged
        private Transform _originalParent;
        private Vector3 _originalPosition;
        private int _originalSiblingIndex;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        // --- HOVER EFFECTS ---
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Simple hover effect: scale up slightly
            transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Reset scale when mouse leaves
            transform.localScale = Vector3.one;
        }

        // --- DRAG AND DROP ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Remember where we started
            _originalParent = transform.parent;
            _originalPosition = transform.position;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Pop it out of the hand layout group so it can move freely
            transform.SetParent(transform.root); // Move to main canvas level
            transform.SetAsLastSibling(); // Ensure it renders on top of everything

            // Make it slightly transparent while dragging and ignore raycasts so we can drop it ON something (like a Hex board)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.8f;
                canvasGroup.blocksRaycasts = false; 
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Make the card follow the mouse position
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Reset visuals
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // NOTE: Here is where you would check if it was dropped on a valid "Hex Tile" or "Play Zone".
            // Since we don't have Abdo's Hex Board yet, we will just snap it back to the hand.
            
            bool validDrop = false; // Placeholder

            if (!validDrop)
            {
                // Snap back to hand
                transform.SetParent(_originalParent);
                transform.SetSiblingIndex(_originalSiblingIndex);
                transform.position = _originalPosition;
                Debug.Log("Invalid drop! Snapping card back to hand.");
            }
            else
            {
                // Card was played successfully
                Debug.Log($"Card {cardNameText?.text} was played!");
                // Destroy(gameObject); // Or send it to graveyard
            }
        }
    }
}
