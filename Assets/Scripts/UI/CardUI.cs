using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace FortGame.UI
{
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Card Visuals")]
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI costText;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public CardRuntimeState runtimeCard;

        [Header("Selection Visuals")]
        public Color selectedColor = new Color(1f, 1f, 0f, 1f);

        [Header("Lift Animation")]
        [Tooltip("The RectTransform on the HUD canvas that the card flies to when selected. Create an empty child of your HUD Canvas and assign it here.")]
        public RectTransform hudSelectedCardAnchor;

        [Tooltip("How long the fly-to animation takes in seconds.")]
        public float liftDuration = 0.2f;

        [Tooltip("How much bigger the card becomes when lifted (1.4 = 40% bigger).")]
        public float liftScale = 1.4f;

        // ── Private state ─────────────────────────────────────────────────
        private GameManager _gameManager;
        private HUDManager _hudManager;
        private Image _imageComponent;
        private Color _originalColor;
        private bool _isSelected;
        private Coroutine _animCoroutine;
        private LayoutElement _layoutElement;

        // Saved so we can return exactly to where the card was
        private Vector2 _savedAnchoredPosition;
        private Vector3 _savedScale;
        private bool _positionSaved;

        public Action<CardUI> clickOverride;

        public string CardName => cardNameText?.text ?? "Unknown";
        public bool IsSelected => _isSelected;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _imageComponent = GetComponent<Image>();
            if (_imageComponent != null)
                _originalColor = _imageComponent.color;

            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();

            _gameManager = FindFirstObjectByType<GameManager>();
            _hudManager  = FindFirstObjectByType<HUDManager>();

            if (hudSelectedCardAnchor == null)
            {
                GameObject anchorObj = GameObject.Find("SelectedCardAnchor");
                if (anchorObj != null)
                {
                    hudSelectedCardAnchor = anchorObj.GetComponent<RectTransform>();
                }
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(selected ? AnimateLift() : AnimateReturn());

            Debug.Log($"[CardUI] {CardName} selection set to {selected}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickOverride != null)
            {
                clickOverride(this);
                return;
            }

            RevivalManager revivalManager = RevivalManager.Instance ?? RevivalManager.GetOrCreate();
            if (revivalManager != null)
            {
                if (revivalManager.BlocksCardSelection(this))
                {
                    _hudManager?.ShowInfo("Finish or cancel Revival before selecting another card.");
                    return;
                }

                if (runtimeCard != null && revivalManager.TryBeginFromHand(runtimeCard))
                    return;
            }

            if (_gameManager != null
                && _gameManager.currentPhase == GamePhase.Buy
                && !_gameManager.isBuyDecisionPending)
            {
                SelectForDiscard();
                return;
            }

            bool selected = CardSelectionManager.Instance?.TrySelectCard(this) ?? false;
            if (selected)
            {
                TargetSelectionManager.Instance?.ShowValidTargets(this);
                return;
            }

            if (!(CardSelectionManager.Instance?.HasSelection ?? false))
                TargetSelectionManager.Instance?.OnSelectionCancelled();
        }

        private void SelectForDiscard()
        {
            if (runtimeCard == null)
            {
                _hudManager?.ShowError("This card is missing game data.");
                Debug.Log("This UI card has no runtime card linked.");
                return;
            }

            _gameManager.SelectCardToDiscard(runtimeCard);
            if (_gameManager.handUI != null)
                _gameManager.handUI.ClearVisualSelection();

            SetSelected(true);
            _hudManager?.SetSelectedCard(CardName);
            _hudManager?.ShowInfo($"{CardName} selected for discard.");
            Debug.Log("Card selected for discard.");
        }

        private IEnumerator AnimateLift()
        {
            _savedAnchoredPosition = rectTransform.anchoredPosition;
            _savedScale = transform.localScale;
            _positionSaved = true;

            _layoutElement.ignoreLayout = true;

            Canvas cardCanvas = gameObject.GetComponent<Canvas>();
            if (cardCanvas == null)
            {
                cardCanvas = gameObject.AddComponent<Canvas>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = 100;

            Vector2 targetPos = GetTargetLocalPosition();
            Vector3 targetScale = Vector3.one * liftScale;

            if (hudSelectedCardAnchor == null)
            {
                GameObject anchorObj = GameObject.Find("SelectedCardAnchor");
                if (anchorObj != null)
                {
                    hudSelectedCardAnchor = anchorObj.GetComponent<RectTransform>();
                }
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && hudSelectedCardAnchor != null)
            {
                StatsPanelUI statsPanel = StatsPanelUI.GetOrCreate(parentCanvas);
                if (statsPanel != null)
                {
                    statsPanel.Show(runtimeCard, hudSelectedCardAnchor, liftScale, liftDuration);
                }
            }

            yield return SmoothMove(targetPos, targetScale, liftDuration);
        }

        private IEnumerator AnimateReturn()
        {
            if (!_positionSaved)
            {
                transform.localScale = Vector3.one;
                yield break;
            }

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                StatsPanelUI statsPanel = StatsPanelUI.GetOrCreate(parentCanvas);
                if (statsPanel != null)
                {
                    statsPanel.Hide(liftDuration * 0.85f);
                }
            }

            yield return SmoothMove(_savedAnchoredPosition, _savedScale, liftDuration * 0.85f);

            _layoutElement.ignoreLayout = false;
            _positionSaved = false;

            Canvas cardCanvas = gameObject.GetComponent<Canvas>();
            if (cardCanvas != null)
            {
                Destroy(gameObject.GetComponent<GraphicRaycaster>());
                Destroy(cardCanvas);
            }

            rectTransform.anchoredPosition = _savedAnchoredPosition;
            transform.localScale = _savedScale;
        }

        private IEnumerator SmoothMove(Vector2 targetPos, Vector3 targetScale, float duration)
        {
            Vector2 startPos   = rectTransform.anchoredPosition;
            Vector3 startScale = transform.localScale;
            float elapsed      = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                transform.localScale           = Vector3.Lerp(startScale, targetScale, ease);
                yield return null;
            }

            rectTransform.anchoredPosition = targetPos;
            transform.localScale           = targetScale;
        }

        private Vector2 GetTargetLocalPosition()
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null) return rectTransform.anchoredPosition;

            GameObject realAnchor = GameObject.Find("SelectedCardAnchor");
            
            if (realAnchor != null)
            {
                Vector3 localPos3D = parentRect.InverseTransformPoint(realAnchor.transform.position);
                return new Vector2(localPos3D.x, localPos3D.y);
            }

            Debug.LogError("unfound gameobject (anchor) in the current scene");
            return new Vector2(400f, 80f); // Default fallback
        }
    }
}