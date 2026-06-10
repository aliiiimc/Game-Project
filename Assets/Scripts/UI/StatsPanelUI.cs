using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FortGame.UI
{
    public class StatsPanelUI : MonoBehaviour
    {
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _statsText;
        private CanvasGroup _canvasGroup;
        private Coroutine _animCoroutine;

        public static StatsPanelUI GetOrCreate(Canvas canvas)
        {
            if (canvas == null) return null;
            
            Transform existing = canvas.transform.Find("StatsPanel");
            if (existing != null)
            {
                return existing.GetComponent<StatsPanelUI>();
            }

            GameObject panelObj = new GameObject("StatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(StatsPanelUI));
            panelObj.transform.SetParent(canvas.transform, false);

            StatsPanelUI statsPanel = panelObj.GetComponent<StatsPanelUI>();
            statsPanel.Initialize();
            return statsPanel;
        }

        private void Initialize()
        {
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f); // Anchors set to middle-right
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(250f, 180f);
            rect.anchoredPosition = Vector2.zero;

            Image image = GetComponent<Image>();
            Sprite sprite = Resources.Load<Sprite>("UI/HUD/Stats_Panel");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.color = new Color(0.12f, 0.15f, 0.20f, 0.95f);
            }

            // Create title text
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -15f);
            titleRect.sizeDelta = new Vector2(-30f, 30f);

            _titleText = titleObj.GetComponent<TextMeshProUGUI>();
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = 20f;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = new Color(0.96f, 0.78f, 0.26f, 1f); // Golden yellow
            _titleText.text = "Card Name";
            ApplyFont(_titleText);

            // Create stats text
            GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(transform, false);
            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.offsetMin = new Vector2(20f, 15f);
            statsRect.offsetMax = new Vector2(-20f, -45f);

            _statsText = statsObj.GetComponent<TextMeshProUGUI>();
            _statsText.alignment = TextAlignmentOptions.Center;
            _statsText.fontSize = 16f;
            _statsText.color = Color.white;
            _statsText.lineSpacing = 6f;
            _statsText.text = "Stats info";
            ApplyFont(_statsText);

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null) return;
            
            // Try to find any existing TextMeshProUGUI in the scene to copy its font
            TextMeshProUGUI templateText = FindFirstObjectByType<TextMeshProUGUI>();
            if (templateText != null)
            {
                text.font = templateText.font;
                text.fontSharedMaterial = templateText.fontSharedMaterial;
            }
        }

        public void Show(CardRuntimeState card, RectTransform cardAnchor, float liftScale, float duration)
        {
            if (card == null || cardAnchor == null) return;

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            gameObject.SetActive(true);
            PopulateStats(card);

            _animCoroutine = StartCoroutine(AnimateShow(cardAnchor, liftScale, duration));
        }

        public void Hide(float duration)
        {
            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(AnimateHide(duration));
        }

        private void PopulateStats(CardRuntimeState card)
        {
            if (card.SourceCard == null) return;
            
            _titleText.text = card.SourceCard.DisplayName;

            CardData data = card.SourceCard;
            if (data is CharacterCardData charData)
            {
                string speedText = charData.unitMovementCapacity.HasValue ? charData.unitMovementCapacity.Value.ToString() : "N/A";
                _statsText.text = $"HP: {charData.maxHp}\nAttack: {charData.attackDamage} (Range: {charData.attackRange})\nSpeed: {speedText}";
            }
            else if (data is WorldEffectCardData weData)
            {
                List<string> lines = new List<string>();
                if (weData.structureHp.HasValue) lines.Add($"HP: {weData.structureHp.Value}");
                if (weData.structureDamage.HasValue) lines.Add($"Attack: {weData.structureDamage.Value}");
                if (weData.revenuePerTurn.HasValue) lines.Add($"Income: +{weData.revenuePerTurn.Value} Gold");
                if (weData.durationTurns > 0) lines.Add($"Duration: {weData.durationTurns} Turns");
                _statsText.text = lines.Count > 0 ? string.Join("\n", lines) : "Special Structure";
            }
            else if (data is SpellCardData spellData)
            {
                List<string> lines = new List<string>();
                lines.Add($"Power: {spellData.effectPower}");
                if (spellData.effectDurationTurns > 0) lines.Add($"Duration: {spellData.effectDurationTurns} Turns");
                _statsText.text = string.Join("\n", lines);
            }
            else
            {
                _statsText.text = data.description;
            }
        }

        private IEnumerator AnimateShow(RectTransform cardAnchor, float liftScale, float duration)
        {
            RectTransform rect = GetComponent<RectTransform>();
            
            // Align base position with cardAnchor X
            rect.anchoredPosition = new Vector2(cardAnchor.anchoredPosition.x, rect.anchoredPosition.y);
            
            float targetY = cardAnchor.anchoredPosition.y + (cardAnchor.rect.height * 0.5f * liftScale) + (rect.rect.height * 0.5f) + 15f;
            float startY = targetY + 50f; // Start 50 units higher and slide down

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(startY, targetY, ease));
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator AnimateHide(float duration)
        {
            RectTransform rect = GetComponent<RectTransform>();
            float startY = rect.anchoredPosition.y;
            float targetY = startY + 50f; // Slide up as it fades out

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(startY, targetY, ease));
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, ease);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
