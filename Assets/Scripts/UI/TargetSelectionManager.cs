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
        private GameManager _gameManager;
        private CardPlayService _cardPlayService;

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
            _gameManager = FindFirstObjectByType<GameManager>();
            _cardPlayService = FindFirstObjectByType<CardPlayService>();
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

            if (_hexGrid == null)
            {
                _hexGrid = FindFirstObjectByType<HexGrid>();
            }

            if (_cardPlayService == null)
            {
                _cardPlayService = FindFirstObjectByType<CardPlayService>();
            }

            if (_hexGrid == null || _cardPlayService == null || card.runtimeCard == null)
            {
                _hudManager?.ShowError("Target selection is not ready.");
                return;
            }

            string actingPlayerKey = ResolveCurrentPlayerKey();
            for (int r = 0; r < _hexGrid.gridHeight; r++)
            {
                for (int q = 0; q < _hexGrid.gridWidth; q++)
                {
                    HexTile tile = _hexGrid.GetTile(new AxialCoord(q, r));
                    if (tile == null)
                    {
                        continue;
                    }

                    CardTarget target = new CardTarget
                    {
                        type = CardTargetType.Tile,
                        tile = new AxialCoord(tile.coord.q, tile.coord.r)
                    };

                    // Rabie: highlight only targets that the real card play pipeline says are legal.
                    CardPlayResult result = _cardPlayService.CanPlayCard(card.runtimeCard, actingPlayerKey, target);
                    if (result.Succeeded)
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
            // Rabie: send the confirmed tile to PlayerInputController so the selected card is actually played.
            PlayerInputController.Instance?.OnTargetConfirmed(target);

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

        private string ResolveCurrentPlayerKey()
        {
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_gameManager == null || _gameManager.currentPlayer == null)
            {
                return string.Empty;
            }

            if (ReferenceEquals(_gameManager.currentPlayer, _gameManager.player1))
            {
                return PlayerKeyResolver.PlayerOneKey;
            }

            if (ReferenceEquals(_gameManager.currentPlayer, _gameManager.player2))
            {
                return PlayerKeyResolver.PlayerTwoKey;
            }

            return _gameManager.currentPlayer.playerName;
        }
    }
}
