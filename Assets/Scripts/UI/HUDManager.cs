using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; //Ali : ajouté parce que le bouton Restart va recharger la scène actuelle avec: SceneManager.LoadScene(...)
using TMPro; // Standard Unity text package

namespace FortGame.UI
{
    /// <summary>
    /// Manages the heads-up display showing the player's Fort HP, Resources/Money, and game messages.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Legacy UI Elements")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI fortHpText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI turnStatusText;
        public TextMeshProUGUI selectedCardText;
        public TextMeshProUGUI infoMessageText;
        public TextMeshProUGUI errorMessageText;
        public float errorMessageDuration = 3f;

        [Header("Player Panel")]
        public TextMeshProUGUI playerFortHpText;
        public TextMeshProUGUI playerMoneyText;
        public TextMeshProUGUI playerCardsText;
        public Image playerHpFill;

        [Header("Enemy Panel")]
        public TextMeshProUGUI enemyFortHpText;
        public TextMeshProUGUI enemyMoneyText;
        public TextMeshProUGUI enemyCardsText;
        public Image enemyHpFill;

        [Header("Turn Panel")]
        public TextMeshProUGUI currentRoundText;
        public TextMeshProUGUI currentPlayerText;

        [Header("Spell Announcement")]
        public RectTransform upperPanelTransform;
        public TextMeshProUGUI spellAnnouncementText;
        public float spellAnnouncementDuration = 2.5f;

        [Header("Game Over")]
        public GameObject gameOverPanel; //Ali : gameOverPanel: le panel entier à afficher/cacher.
        public TextMeshProUGUI winnerText; // Ali : Winner announcement text
        public GameObject gameplayControlsRoot; // Ali : Pour cacher boutons quand gameover 

        private float _errorMessageTimer = 0f;
        private Coroutine _spellBannerCoroutine = null;
        private Vector2 _upperPanelVisiblePos;
        private Vector2 _upperPanelHiddenPos;
        private float _spellAnnouncementTimer = 0f;

        private void Awake()
        {
            AutoBindMissingReferences();

            if (upperPanelTransform != null && spellAnnouncementText == null)
            {
                spellAnnouncementText = upperPanelTransform.GetComponentInChildren<TextMeshProUGUI>();
                
                if (spellAnnouncementText == null)
                {
                    GameObject textObj = new GameObject("BannerText");
                    textObj.transform.SetParent(upperPanelTransform, false);
                    
                    RectTransform rect = textObj.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    
                    spellAnnouncementText = textObj.AddComponent<TextMeshProUGUI>();
                    spellAnnouncementText.alignment = TextAlignmentOptions.Center;
                    spellAnnouncementText.color = Color.white;
                    spellAnnouncementText.text = string.Empty;
                    spellAnnouncementText.enableAutoSizing = true;
                    spellAnnouncementText.fontSizeMin = 18f;
                    spellAnnouncementText.fontSizeMax = 40f;
                    
                    if (currentPlayerText != null)
                    {
                        spellAnnouncementText.font = currentPlayerText.font;
                        spellAnnouncementText.fontSharedMaterial = currentPlayerText.fontSharedMaterial;
                    }
                }
            }

            EnsureSpellAnnouncementBinding();
            SetGameOverPanelVisible(false); // Au lancement de la scène, la partie n’est pas finie. Donc le panel Game Over doit être caché dès le début.

            if (upperPanelTransform != null)
            {
                _upperPanelVisiblePos = upperPanelTransform.anchoredPosition;
                _upperPanelHiddenPos = new Vector2(_upperPanelVisiblePos.x, _upperPanelVisiblePos.y + upperPanelTransform.rect.height + 50f);
                upperPanelTransform.anchoredPosition = _upperPanelHiddenPos;
            }
        }

        private void Update()
        {
            // Fade out error message after duration
            if (_errorMessageTimer > 0)
            {
                _errorMessageTimer -= Time.deltaTime;
                if (_errorMessageTimer <= 0 && errorMessageText != null)
                {
                    errorMessageText.text = "";
                }
            }

            if (_spellAnnouncementTimer > 0)
            {
                _spellAnnouncementTimer -= Time.deltaTime;
                if (_spellAnnouncementTimer <= 0 && spellAnnouncementText != null)
                {
                    spellAnnouncementText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the HUD to reflect the current state of the provided PlayerState.
        /// </summary>
        public void UpdateHUD(PlayerState state)
        {
            if (state == null) return;

            if (playerNameText != null)
                playerNameText.text = state.playerName;

            if (fortHpText != null)
                fortHpText.text = $"Fort HP: {state.fortHp}";

            if (moneyText != null)
                moneyText.text = $"Money: {state.money}";
        }

        public void UpdateHUD(PlayerState player, PlayerState enemy, PlayerState currentPlayer, GamePhase phase, int maxFortHp, int roundNumber, string winnerName)
        {
            AutoBindMissingReferences();
            EnsureSpellAnnouncementBinding();

            UpdatePanel(player, playerFortHpText, playerMoneyText, playerCardsText, playerHpFill, maxFortHp);
            UpdatePanel(enemy, enemyFortHpText, enemyMoneyText, enemyCardsText, enemyHpFill, maxFortHp);

            if (currentRoundText != null)
            {
                currentRoundText.text = $"ROUND {Mathf.Max(1, roundNumber)}";
            }

            if (currentPlayerText != null)
            {
                if (phase == GamePhase.GameOver)
                {
                    currentPlayerText.text = "Game Over";
                }
                else if (currentPlayer != null && player != null && ReferenceEquals(currentPlayer, player))
                {
                    currentPlayerText.text = "Your turn";
                }
                else if (currentPlayer != null && enemy != null && ReferenceEquals(currentPlayer, enemy))
                {
                    currentPlayerText.text = "Enemy turn";
                }
                else
                {
                    currentPlayerText.text = "";
                }
            }

            if (turnStatusText != null && currentPlayer != null)
            {
                turnStatusText.text = $"{currentPlayer.playerName} - {phase}";
            }
            UpdateGameOverUI(phase, winnerName);
        }

        /// <summary>
        /// Call this when the turn changes (e.g., "Your Turn!" or "Enemy Turn")
        /// </summary>
        public void SetTurnStatus(string statusMessage)
        {
            if (turnStatusText != null)
            {
                turnStatusText.text = statusMessage;
            }
        }

        public void SetSelectedCard(string cardName)
        {
            if (selectedCardText != null)
            {
                selectedCardText.text = string.IsNullOrWhiteSpace(cardName)
                    ? ""
                    : $"Selected: {cardName}";
            }
        }

        public void ShowInfo(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
            else if (errorMessageText != null)
            {
                errorMessageText.text = message;
                _errorMessageTimer = 0f;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                Debug.Log($"[HUDManager] Info: {message}");
            }
        }

        public void ShowSpellAnnouncement(string message)
        {
            EnsureSpellAnnouncementBinding();
            if (spellAnnouncementText != null)
            {
                spellAnnouncementText.text = message ?? string.Empty;
            }

            if (upperPanelTransform != null)
            {
                if (_spellBannerCoroutine != null)
                {
                    StopCoroutine(_spellBannerCoroutine);
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    _spellBannerCoroutine = StartCoroutine(AnimateUpperPanel());
                }
                else
                {
                    upperPanelTransform.anchoredPosition = _upperPanelHiddenPos;
                }
            }
            else
            {
                _spellAnnouncementTimer = string.IsNullOrWhiteSpace(message) ? 0f : spellAnnouncementDuration;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                Debug.Log($"[HUDManager] Spell: {message}");
            }
        }

        private IEnumerator AnimateUpperPanel()
        {
            float slideDuration = 0.3f;
            float elapsedTime = 0f;

            // Slide down
            Vector2 startPos = upperPanelTransform.anchoredPosition;
            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / slideDuration);
                upperPanelTransform.anchoredPosition = Vector2.Lerp(startPos, _upperPanelVisiblePos, t);
                yield return null;
            }
            upperPanelTransform.anchoredPosition = _upperPanelVisiblePos;

            // Wait
            yield return new WaitForSeconds(spellAnnouncementDuration);

            // Slide up
            elapsedTime = 0f;
            startPos = upperPanelTransform.anchoredPosition;
            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / slideDuration);
                upperPanelTransform.anchoredPosition = Vector2.Lerp(startPos, _upperPanelHiddenPos, t);
                yield return null;
            }
            upperPanelTransform.anchoredPosition = _upperPanelHiddenPos;

            if (spellAnnouncementText != null)
            {
                spellAnnouncementText.text = string.Empty;
            }
            _spellBannerCoroutine = null;
        }

        public void ClearFeedback()
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = "";
            }

            if (errorMessageText != null)
            {
                errorMessageText.text = "";
            }

            _errorMessageTimer = 0f;
        }

        /// <summary>
        /// Display an error message to the player.
        /// </summary>
        public void ShowError(string message)
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = message;
                _errorMessageTimer = string.IsNullOrWhiteSpace(message) ? 0f : errorMessageDuration;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Debug.Log($"[HUDManager] Error: {message}");
                }
            }
        }

        private void UpdatePanel(
            PlayerState state,
            TextMeshProUGUI fortHpValue,
            TextMeshProUGUI moneyValue,
            TextMeshProUGUI cardsValue,
            Image hpFill,
            int maxFortHp)
        {
            if (state == null)
            {
                return;
            }

            int safeMaxFortHp = Mathf.Max(1, maxFortHp);

            if (fortHpValue != null)
            {
                fortHpValue.text = $"{state.fortHp}/{safeMaxFortHp}";
            }

            if (moneyValue != null)
            {
                moneyValue.text = state.money.ToString();
            }

            if (cardsValue != null)
            {
                cardsValue.text = $"{state.handCount}/{state.maxHandSize}";
            }

            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(state.fortHp / (float)safeMaxFortHp);
            }
        }




        //Ali : UpdateGameOverUI: décide quoi afficher selon la phase.
        private void UpdateGameOverUI(GamePhase phase, string winnerName)
        {
            bool isGameOver = phase == GamePhase.GameOver;

            SetGameOverPanelVisible(isGameOver);

            if (winnerText != null)
            {
                winnerText.text = isGameOver && !string.IsNullOrWhiteSpace(winnerName)
                    ? $"{winnerName} wins!"
                    : "";
            }

            if (gameplayControlsRoot != null)
            {
                gameplayControlsRoot.SetActive(!isGameOver);
            }
        }

        private void SetGameOverPanelVisible(bool visible)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(visible);
            }
        }

        public void RestartCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }




        private void AutoBindMissingReferences()
        {
            if (playerFortHpText == null) playerFortHpText = FindComponentByObjectName<TextMeshProUGUI>("PlayerFortHpText");
            if (playerMoneyText == null) playerMoneyText = FindComponentByObjectName<TextMeshProUGUI>("PlayerMoneyText");
            if (playerCardsText == null) playerCardsText = FindComponentByObjectName<TextMeshProUGUI>("PlayerCardsText");
            if (playerHpFill == null) playerHpFill = FindComponentByObjectName<Image>("PlayerHpFill");

            if (enemyFortHpText == null) enemyFortHpText = FindComponentByObjectName<TextMeshProUGUI>("EnemyFortHpText");
            if (enemyMoneyText == null) enemyMoneyText = FindComponentByObjectName<TextMeshProUGUI>("EnemyMoneyText");
            if (enemyCardsText == null) enemyCardsText = FindComponentByObjectName<TextMeshProUGUI>("EnemyCardsText");
            if (enemyHpFill == null) enemyHpFill = FindComponentByObjectName<Image>("EnemyHpFill");

            if (currentRoundText == null) currentRoundText = FindComponentByObjectName<TextMeshProUGUI>("CurrentRoundText");
            if (currentPlayerText == null) currentPlayerText = FindComponentByObjectName<TextMeshProUGUI>("CurrentPlayerText");
        }

        private void EnsureSpellAnnouncementBinding()
        {
            if (spellAnnouncementText == null) spellAnnouncementText = FindComponentByObjectName<TextMeshProUGUI>("SpellAnnouncementText");
            if (spellAnnouncementText == null) spellAnnouncementText = CreateRuntimeFeedbackText("SpellAnnouncementText", -42f);
        }

        private TextMeshProUGUI CreateRuntimeFeedbackText(string objectName, float verticalOffset)
        {
            if (currentPlayerText == null)
            {
                return null;
            }

            Canvas parentCanvas = currentPlayerText.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                return null;
            }

            Transform existing = FindChildByName(parentCanvas.transform, objectName);
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
            {
                return existingText;
            }

            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parentCanvas.transform, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            RectTransform sourceRect = currentPlayerText.rectTransform;
            rect.anchorMin = sourceRect.anchorMin;
            rect.anchorMax = sourceRect.anchorMax;
            rect.pivot = sourceRect.pivot;
            rect.sizeDelta = new Vector2(Mathf.Max(420f, sourceRect.sizeDelta.x * 1.8f), Mathf.Max(42f, sourceRect.sizeDelta.y));
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, verticalOffset);
            rect.localScale = Vector3.one;

            TextMeshProUGUI feedbackText = textObject.AddComponent<TextMeshProUGUI>();
            feedbackText.font = currentPlayerText.font;
            feedbackText.fontSharedMaterial = currentPlayerText.fontSharedMaterial;
            feedbackText.fontSize = Mathf.Max(20f, currentPlayerText.fontSize * 0.65f);
            feedbackText.enableAutoSizing = false;
            feedbackText.color = currentPlayerText.color;
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.text = string.Empty;
            feedbackText.raycastTarget = false;
            feedbackText.overflowMode = TextOverflowModes.Ellipsis;

            return feedbackText;
        }

        private T FindComponentByObjectName<T>(string objectName) where T : Component
        {
            Transform child = FindChildByName(transform, objectName);
            if (child != null && child.TryGetComponent(out T localComponent))
            {
                return localComponent;
            }

            T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].name == objectName)
                {
                    return components[i];
                }
            }

            return null;
        }

        private Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }
    }
}
