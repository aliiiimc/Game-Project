using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortGame.UI
{
    [RequireComponent(typeof(Button))]
    public class SoundMuteToggle : MonoBehaviour
    {
        [Header("UI Components (Auto-assigned if left empty)")]
        public TextMeshProUGUI buttonText;
        public Text legacyText;
        public Image buttonImage;

        [Header("Sprites (Optional)")]
        public Sprite soundOnSprite;
        public Sprite soundOffSprite;

        [Header("Texts")]
        public string soundOnText = "🔊";
        public string soundOffText = "🔇";

        private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();

            // Auto-detect UI elements if they are not manually assigned
            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (legacyText == null)
            {
                legacyText = GetComponentInChildren<Text>();
            }

            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            // Register click listener
            if (_button != null)
            {
                _button.onClick.AddListener(OnToggleClicked);
            }

            // Sync visual representation with current sound state
            UpdateVisuals();
        }

        private void OnEnable()
        {
            // Update visuals whenever the object becomes active, 
            // ensuring it reflects the globally persisted SoundManager state
            UpdateVisuals();
        }

        private void OnToggleClicked()
        {
            SoundManager soundManager = SoundManager.GetOrCreate();
            if (soundManager != null)
            {
                soundManager.ToggleMute();
                UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            SoundManager soundManager = SoundManager.GetOrCreate();
            if (soundManager == null)
            {
                return;
            }

            bool isMuted = soundManager.IsMuted;

            // 1. Update TextMeshPro Text
            if (buttonText != null)
            {
                buttonText.text = isMuted ? soundOffText : soundOnText;
            }

            // 2. Update Legacy Text
            if (legacyText != null)
            {
                legacyText.text = isMuted ? soundOffText : soundOnText;
            }

            // 3. Update Button Sprite (if sprite assets are configured)
            if (buttonImage != null)
            {
                if (isMuted && soundOffSprite != null)
                {
                    buttonImage.sprite = soundOffSprite;
                }
                else if (!isMuted && soundOnSprite != null)
                {
                    buttonImage.sprite = soundOnSprite;
                }
            }
        }
    }
}
