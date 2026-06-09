using UnityEngine;
using UnityEngine.EventSystems;

namespace FortGame.UI
{
    public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.04f;
        public float pressedScale = 0.97f;
        public float lerpSpeed = 12f;

        private RectTransform _rectTransform;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _isHovered;
        private bool _isPressed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                _baseScale = _rectTransform.localScale;
                _targetScale = _baseScale;
            }
        }

        private void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                _rectTransform.localScale = _baseScale;
                return;
            }

            Vector3 desiredScale = _baseScale;
            if (_isPressed)
            {
                desiredScale = _baseScale * pressedScale;
            }
            else if (_isHovered)
            {
                desiredScale = _baseScale * hoverScale;
            }

            _targetScale = desiredScale;
            _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _targetScale, Time.unscaledDeltaTime * lerpSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }
    }
}
