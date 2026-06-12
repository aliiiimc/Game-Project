using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FortGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StatsPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI statsText;
        public CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("If true, the panel flies from the clicked card's position. If false, it uses startingOffset.")]
        public bool flyFromCard = true;

        [Tooltip("Starting position offset relative to target position when flyFromCard is false.")]
        public Vector2 startingOffset = new Vector2(0f, -50f);

        [Tooltip("If true, the panel is positioned dynamically relative to the preview card. If false, it keeps its editor-configured position.")]
        public bool useDynamicPositioning = true;

        private Vector2 _targetAnchoredPos;
        private RectTransform _rectTransform;
        private Coroutine _animCoroutine;
        private bool _isInitialized;
        private Canvas _canvas;
        private bool _isDynamic = false;

        public static StatsPanelUI GetOrCreate(Canvas canvas)
        {
            if (canvas == null)
            {
                Debug.LogWarning("[StatsPanelUI] GetOrCreate called with null canvas.");
                return null;
            }

            // 1. Search for any existing StatsPanelUI under the Canvas
            StatsPanelUI[] panels = canvas.GetComponentsInChildren<StatsPanelUI>(true);
            StatsPanelUI selectedPanel = null;

            // First, prefer any panel that is already marked as dynamic
            foreach (var panel in panels)
            {
                if (panel != null && panel._isDynamic)
                {
                    selectedPanel = panel;
                    break;
                }
            }

            // If none is marked dynamic, take the first existing scene panel
            if (selectedPanel == null)
            {
                foreach (var panel in panels)
                {
                    if (panel != null)
                    {
                        selectedPanel = panel;
                        Debug.Log($"[StatsPanelUI] Found existing scene StatsPanel: {panel.gameObject.name}. Reusing it.");
                        break;
                    }
                }
            }

            // 2. Destroy any DUPLICATE/obsolete StatsPanels in the scene recursively to avoid conflict
            List<GameObject> toDestroy = new List<GameObject>();
            FindObsoleteStatsPanelsRecursive(canvas.transform, toDestroy, selectedPanel != null ? selectedPanel.gameObject : null);
            foreach (var obj in toDestroy)
            {
                if (obj != null)
                {
                    Debug.Log($"[StatsPanelUI] Destroying duplicate StatsPanel GameObject: {obj.name} (parent: {(obj.transform.parent != null ? obj.transform.parent.name : "none")})");
                    Destroy(obj);
                }
            }

            // 3. Create a fresh dynamic panel from scratch if it doesn't exist
            if (selectedPanel == null)
            {
                Debug.Log("[StatsPanelUI] Creating fresh dynamic StatsPanel from scratch.");
                GameObject newPanelObj = new GameObject("StatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                newPanelObj.transform.SetParent(canvas.transform, false);
                
                // Sync layer with canvas to make sure it renders on the UI layer (Layer 5)
                newPanelObj.layer = canvas.gameObject.layer;

                selectedPanel = newPanelObj.AddComponent<StatsPanelUI>();
                selectedPanel._isDynamic = true;
            }

            selectedPanel.AttachToCanvas(canvas);
            selectedPanel.Initialize();
            return selectedPanel;
        }

        private static void FindObsoleteStatsPanelsRecursive(Transform parent, List<GameObject> results, GameObject exclude)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == "StatsPanel" && child.gameObject != exclude)
                {
                    results.Add(child.gameObject);
                }
                else
                {
                    FindObsoleteStatsPanelsRecursive(child, results, exclude);
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _targetAnchoredPos = _rectTransform.anchoredPosition;
            AttachToCanvas(GetComponentInParent<Canvas>(true));

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Only initialize dynamically if it is marked dynamic, or if the references are missing
            if (_isDynamic || statsText == null)
            {
                InitializeDynamicUI();
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
            _isInitialized = true;
        }

        private void AttachToCanvas(Canvas targetCanvas)
        {
            if (targetCanvas != null && targetCanvas.transform != transform && transform.parent != targetCanvas.transform)
            {
                transform.SetParent(targetCanvas.transform, false);
            }

            transform.SetAsLastSibling();

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = Quaternion.identity;

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 260;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void InitializeDynamicUI()
        {
            // Setup base panel dimensions if not already configured
            if (_rectTransform.sizeDelta.x <= 10f || _rectTransform.sizeDelta.y <= 10f)
            {
                _rectTransform.sizeDelta = new Vector2(250f, 180f);
            }

            // Setup background image with the official texture if not set
            Image image = GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                Sprite sprite = Resources.Load<Sprite>("UI/HUD/Stats_Panel");
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }
                else
                {
                    // Fallback sleek color if resource isn't found
                    image.color = new Color(0.12f, 0.15f, 0.20f, 0.95f);
                }
            }

            // Attempt to resolve statsText reference if missing
            if (statsText == null)
            {
                statsText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (statsText == null)
            {
                GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                statsObj.transform.SetParent(transform, false);
                statsObj.layer = gameObject.layer; // Sync layer with parent
                RectTransform statsRect = statsObj.GetComponent<RectTransform>();
                statsRect.anchorMin = new Vector2(0f, 0f);
                statsRect.anchorMax = new Vector2(1f, 1f);
                statsRect.pivot = new Vector2(0.5f, 0.5f);
                statsRect.offsetMin = new Vector2(20f, 15f);
                statsRect.offsetMax = new Vector2(-20f, -15f);

                statsText = statsObj.GetComponent<TextMeshProUGUI>();
                statsText.alignment = TextAlignmentOptions.Center;
                statsText.fontSize = 16f;
                statsText.color = Color.white;
                statsText.lineSpacing = 6f;
                statsText.text = "Stats info";
                ApplyFont(statsText);
            }
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null) return;
            TextMeshProUGUI templateText = FindFirstObjectByType<TextMeshProUGUI>();
            if (templateText != null)
            {
                text.font = templateText.font;
                text.fontSharedMaterial = templateText.fontSharedMaterial;
            }
        }

        public void SyncAnchorsAndPivot(RectTransform source)
        {
            if (source == null) return;
            Initialize();

            Debug.Log($"[StatsPanelUI] SyncAnchorsAndPivot from {source.gameObject.name}. Source AnchorMin={source.anchorMin}, AnchorMax={source.anchorMax}, Pivot={source.pivot}");

            _rectTransform.anchorMin = source.anchorMin;
            _rectTransform.anchorMax = source.anchorMax;
            _rectTransform.pivot = source.pivot;

            if (source.anchorMin != source.anchorMax || _rectTransform.sizeDelta.x <= 10f || _rectTransform.sizeDelta.y <= 10f)
            {
                _rectTransform.sizeDelta = new Vector2(250f, 180f);
            }
        }

        public void Show(CardRuntimeState card, Vector3 cardStartWorldPos, float duration)
        {
            Initialize();

            Debug.Log($"[StatsPanelUI] Show called. Card: {card?.SourceCard?.DisplayName}, StartPos: {cardStartWorldPos}, activeSelf: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy}");

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PopulateStats(card);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f; // Force visible alpha immediately as a fail-safe

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[StatsPanelUI] Show: gameObject.activeInHierarchy is false after SetActive(true).");
            }

            _animCoroutine = StartCoroutine(AnimateShow(cardStartWorldPos, duration));
        }

        public void ShowFromRight(CardRuntimeState card, float duration)
        {
            Initialize();

            Debug.Log($"[StatsPanelUI] ShowFromRight called. Card: {card?.SourceCard?.DisplayName}, activeSelf: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy}");

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PopulateStats(card);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f; // Force visible alpha immediately as a fail-safe

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[StatsPanelUI] ShowFromRight: gameObject.activeInHierarchy is false after SetActive(true).");
            }

            _animCoroutine = StartCoroutine(AnimateShowFromRight(duration));
        }

        public void Hide(float duration)
        {
            Initialize();

            Debug.Log($"[StatsPanelUI] Hide called. activeSelf: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy}");

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            if (!gameObject.activeInHierarchy)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                Debug.Log("[StatsPanelUI] Hide finished instantly (already inactive in hierarchy).");
                return;
            }

            _animCoroutine = StartCoroutine(AnimateHide(duration));
        }

        public void SetTargetAnchoredPosition(Vector2 targetAnchoredPos)
        {
            Initialize();
            Debug.Log($"[StatsPanelUI] SetTargetAnchoredPosition: {targetAnchoredPos}");
            _targetAnchoredPos = targetAnchoredPos;
        }

        private void PopulateStats(CardRuntimeState card)
        {
            if (statsText == null) return;

            if (card == null || card.SourceCard == null)
            {
                statsText.text = string.Empty;
                return;
            }

            CardData data = card.SourceCard;
            if (data is CharacterCardData charData)
            {
                string speedText = charData.unitMovementCapacity.HasValue ? charData.unitMovementCapacity.Value.ToString() : "-";
                
                // Priest special display
                if (charData.specialCardId == SpecialCardIds.CharacterPriest || charData.cardName == "Priest")
                {
                    statsText.text = $"HP : {charData.maxHp}\nHeal : {charData.attackDamage}\nRange : {charData.attackRange}\nMovement : {speedText}";
                }
                // Engineer special display
                else if (charData is EngineerCardData engineer)
                {
                    statsText.text = $"HP : {charData.maxHp}\nRepair : {engineer.structureRepairBoostAmount}\nRange : {charData.attackRange}\nMovement : {speedText}";
                }
                // Special display for UFO Cow to show field consume damage alongside normal attack damage
                else if (charData is UfoCowCardData ufoCow)
                {
                    statsText.text = $"HP : {charData.maxHp}\nDamage : {charData.attackDamage} / {ufoCow.fieldConsumeAmount} against fields\nMovement : {speedText}";
                }
                else
                {
                    string damageLine = charData.attackDamage > 0 ? $"\nDamage : {charData.attackDamage}" : "";
                    statsText.text = $"HP : {charData.maxHp}{damageLine}\nRange : {charData.attackRange}\nMovement : {speedText}";
                }
            }
            else if (data is WorldEffectCardData weData)
            {
                // Healing Station (Hospital) special display
                if (weData is HospitalCardData hospital)
                {
                    string hpText = hospital.structureHp.HasValue ? hospital.structureHp.Value.ToString() : "-";
                    statsText.text = $"HP : {hpText}\nHeal : {hospital.healAmount}\nRange : {hospital.triggerRange}";
                }
                // Wheat Field special display
                else if (weData is WheatFieldCardData wheatField)
                {
                    string hpText = wheatField.structureHp.HasValue ? wheatField.structureHp.Value.ToString() : "-";
                    statsText.text = $"HP : {hpText}\nGold / Turn : {wheatField.bonusMoneyPerTurn}";
                }
                // Wall special display
                else if (weData is WallCardData wall)
                {
                    string hpText = wall.structureHp.HasValue ? wall.structureHp.Value.ToString() : "-";
                    statsText.text = $"HP : {hpText}\nLength : {wall.tilesPerWall}";
                }
                // Special display for Mines to show number of mines and individual mine trigger damage
                else if (weData is MinesCardData mines)
                {
                    statsText.text = $"Mines : {mines.minesToPlace}\nDamage : {mines.mineDamage}";
                }
                else
                {
                    string hpText = weData.structureHp.HasValue ? weData.structureHp.Value.ToString() : "-";
                    string stats = $"HP : {hpText}";
                    if (weData.structureDamage.HasValue && weData.structureDamage.Value > 0)
                    {
                        stats += $"\nDamage : {weData.structureDamage.Value}";
                    }
                    if (weData.worldEffectAttackRange.HasValue && weData.worldEffectAttackRange.Value > 0)
                    {
                        stats += $"\nRange : {weData.worldEffectAttackRange.Value}";
                    }
                    statsText.text = stats;
                }
            }
            else if (data is SpellCardData spellData)
            {
                // Speed Spell special display
                if (spellData is SpeedSpellCardData speedSpell)
                {
                    statsText.text = $"Speed Bonus : +{speedSpell.movementCapacityBonus}\nDuration : {spellData.effectDurationTurns}";
                }
                else if (spellData.effectType == SpellEffectType.Damage)
                {
                    if (spellData.effectDurationTurns > 0)
                    {
                        statsText.text = $"Damage : {spellData.effectPower}\nDuration : {spellData.effectDurationTurns}";
                    }
                    else
                    {
                        statsText.text = $"Damage : {spellData.effectPower}";
                    }
                }
                else
                {
                    if (spellData.effectDurationTurns > 0)
                    {
                        statsText.text = $"Duration : {spellData.effectDurationTurns}";
                    }
                    else
                    {
                        statsText.text = string.Empty;
                    }
                }
            }
            else
            {
                statsText.text = data.description;
            }
        }

        private IEnumerator AnimateShow(Vector3 cardStartWorldPos, float duration)
        {
            RectTransform canvasRect = _rectTransform.parent as RectTransform;

            Vector2 startAnchoredPos;
            if (flyFromCard && canvasRect != null)
            {
                Vector3 localStartPos = canvasRect.InverseTransformPoint(cardStartWorldPos);
                startAnchoredPos = new Vector2(localStartPos.x, localStartPos.y);
            }
            else
            {
                startAnchoredPos = _targetAnchoredPos + startingOffset;
            }

            Debug.Log($"[StatsPanelUI] AnimateShow started. startAnchoredPos: {startAnchoredPos}, targetAnchoredPos: {_targetAnchoredPos}");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, _targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }

            _rectTransform.anchoredPosition = _targetAnchoredPos;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            Debug.Log($"[StatsPanelUI] AnimateShow finished. finalAnchoredPos: {_rectTransform.anchoredPosition}, final alpha: {(canvasGroup != null ? canvasGroup.alpha : 1f)}");
        }

        private IEnumerator AnimateHide(float duration)
        {
            Vector2 startAnchoredPos = _rectTransform.anchoredPosition;
            Vector2 targetAnchoredPos = _targetAnchoredPos + startingOffset;

            Debug.Log($"[StatsPanelUI] AnimateHide started. startAnchoredPos: {startAnchoredPos}, targetAnchoredPos: {targetAnchoredPos}");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, ease);
                yield return null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(false);

            Debug.Log($"[StatsPanelUI] AnimateHide finished. activeSelf: {gameObject.activeSelf}, alpha: {(canvasGroup != null ? canvasGroup.alpha : 0f)}");
        }

        private IEnumerator AnimateShowFromRight(float duration)
        {
            Vector2 startAnchoredPos = _targetAnchoredPos + new Vector2(700f, 0f);

            Debug.Log($"[StatsPanelUI] AnimateShowFromRight started. startAnchoredPos: {startAnchoredPos}, targetAnchoredPos: {_targetAnchoredPos}");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, _targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }

            _rectTransform.anchoredPosition = _targetAnchoredPos;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            Debug.Log($"[StatsPanelUI] AnimateShowFromRight finished. finalAnchoredPos: {_rectTransform.anchoredPosition}, final alpha: {(canvasGroup != null ? canvasGroup.alpha : 1f)}");
        }
    }
}
