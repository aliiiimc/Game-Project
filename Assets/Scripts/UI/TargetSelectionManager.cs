using System.Collections.Generic;
using UnityEngine;

namespace FortGame.UI
{
    /// <summary>
    /// Manages target selection after a card is selected.
    /// Shows valid targets and validates player clicks.
    /// </summary>
    public sealed class TargetSelectionManager : MonoBehaviour
    {
        public static TargetSelectionManager Instance { get; private set; }

        private List<HexTile> _highlightedTiles = new List<HexTile>();
        private HexGrid _hexGrid;
        private HUDManager _hudManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _hexGrid = FindFirstObjectByType<HexGrid>();
            _hudManager = FindFirstObjectByType<HUDManager>();
        }

        /// <summary>
        /// Shows all valid targets for the currently selected card.
        /// </summary>
        public void ShowValidTargets(CardUI card)
        {
            if (card == null)
            {
                ClearHighlights();
                return;
            }

            ClearHighlights();

            CardSelectionManager.Instance?.EnterTargetSelection();

            // For now, highlight all empty tiles as valid targets
            // In Week 3, this will integrate with validator from card data
            for (int r = 0; r < _hexGrid.gridHeight; r++)
            {
                for (int q = 0; q < _hexGrid.gridWidth; q++)
                {
                    HexTile tile = _hexGrid.GetTile(new AxialCoord(q, r));
                    if (tile != null && tile.IsEmpty())
                    {
                        tile.Highlight(new Color(0.2f, 1f, 0.2f, 1f)); // Green
                        _highlightedTiles.Add(tile);
                    }
                }
            }

            Debug.Log($"[TargetSelectionManager] Showing {_highlightedTiles.Count} valid targets for {card.CardName}");
        }

        /// <summary>
        /// Clears all target highlights.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var tile in _highlightedTiles)
            {
                if (tile != null)
                {
                    tile.ResetColor();
                }
            }

            _highlightedTiles.Clear();
        }

        /// <summary>
        /// Processes a tile click as a target selection.
        /// </summary>
        public bool TrySelectTarget(HexTile targetTile)
        {
            CardSelectionManager selectionMgr = CardSelectionManager.Instance;
            if (selectionMgr?.SelectedCard == null)
            {
                return false;
            }

            if (!_highlightedTiles.Contains(targetTile))
            {
                if (_hudManager != null)
                {
                    _hudManager.ShowError("Invalid target selected.");
                }

                Debug.Log("[TargetSelectionManager] Target not in valid targets list.");
                return false;
            }

            CardTarget target = new CardTarget
            {
                type = CardTargetType.Tile,
                tile = new AxialCoord(targetTile.coord.q, targetTile.coord.r)
            };

            selectionMgr.ConfirmSelection(target);

            Debug.Log($"[TargetSelectionManager] Target confirmed at ({targetTile.coord.q}, {targetTile.coord.r})");

            ClearHighlights();

            return true;
        }

        /// <summary>
        /// Called when selection is cancelled to reset highlights.
        /// </summary>
        public void OnSelectionCancelled()
        {
            ClearHighlights();
        }
    }
}
