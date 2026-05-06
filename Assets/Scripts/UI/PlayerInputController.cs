using UnityEngine;

namespace FortGame.UI
{
    /// <summary>
    /// Bridges player UI interactions with game state and action execution.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        public static PlayerInputController Instance { get; private set; }

        private CardSelectionManager _cardSelectionMgr;
        private TargetSelectionManager _targetSelectionMgr;
        private HUDManager _hudManager;
        private GameManager _gameManager;
        private CardPlayService _cardPlayService;
        private HexGrid _hexGrid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _cardSelectionMgr = FindFirstObjectByType<CardSelectionManager>();
            _targetSelectionMgr = FindFirstObjectByType<TargetSelectionManager>();
            _hudManager = FindFirstObjectByType<HUDManager>();
            _gameManager = FindFirstObjectByType<GameManager>();
            _cardPlayService = FindFirstObjectByType<CardPlayService>();
            _hexGrid = FindFirstObjectByType<HexGrid>();
        }

        private void Update()
        {
            // Phase change clears selection
            if (_cardSelectionMgr != null && _gameManager != null)
            {
                if (_gameManager.currentPhase != GamePhase.Play && _cardSelectionMgr.HasSelection)
                {
                    _cardSelectionMgr.ClearSelection();
                    _targetSelectionMgr?.OnSelectionCancelled();
                    _hudManager?.ShowInfo("Selection cleared because the phase changed.");
                }
            }

            // ESC key to cancel selection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_cardSelectionMgr?.HasSelection ?? false)
                {
                    _cardSelectionMgr.CancelSelection();
                    _targetSelectionMgr?.OnSelectionCancelled();
                    _hudManager?.ShowInfo("Selection cancelled.");
                    Debug.Log("[PlayerInputController] Selection cancelled by ESC.");
                }
            }
        }

        /// <summary>
        /// Called when a card's target is confirmed by the player.
        /// </summary>
        public void OnTargetConfirmed(CardTarget target)
        {
            CardUI selectedCard = _cardSelectionMgr?.SelectedCard;
            if (selectedCard == null)
            {
                return;
            }

            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_cardPlayService == null)
            {
                _cardPlayService = FindFirstObjectByType<CardPlayService>();
            }

            if (_gameManager == null || _cardPlayService == null)
            {
                _hudManager?.ShowError("Card play system is not ready.");
                Debug.LogWarning("[PlayerInputController] Missing GameManager or CardPlayService.");
                return;
            }

            CardRuntimeState runtimeCard = selectedCard.runtimeCard;
            if (runtimeCard == null || runtimeCard.SourceCard == null)
            {
                _hudManager?.ShowError("Selected card is missing game data.");
                Debug.LogWarning("[PlayerInputController] Selected CardUI has no runtime card.");
                return;
            }

            PlayerState actingPlayer = _gameManager.currentPlayer;
            if (actingPlayer == null || actingPlayer.handCards == null || !actingPlayer.handCards.Contains(runtimeCard))
            {
                _hudManager?.ShowError("Selected card is not in the current player's hand.");
                Debug.LogWarning("[PlayerInputController] Selected card is not owned by the current player.");
                return;
            }

            int cardCost = runtimeCard.SourceCard.cost;
            if (actingPlayer.money < cardCost)
            {
                _hudManager?.ShowError("Not enough money to play this card.");
                Debug.Log("[PlayerInputController] Current player has insufficient money.");
                return;
            }

            // Rabie: play the selected UI card through the real card pipeline for the current player.
            string actingPlayerKey = ResolveCurrentPlayerKey();
            CardPlayResult result = _cardPlayService.PlayCard(runtimeCard, actingPlayerKey, target);
            if (!result.Succeeded)
            {
                _hudManager?.ShowError(result.Message);
                Debug.Log($"[PlayerInputController] Card play failed: {result.ReasonCode} - {result.Message}");
                return;
            }

            ApplyBoardVisual(runtimeCard, actingPlayerKey, target);

            if (_gameManager.handUI != null)
            {
                _gameManager.handUI.RemoveCardFromHand(runtimeCard);
            }

            _cardSelectionMgr.ClearSelection();
            _targetSelectionMgr?.OnSelectionCancelled();

            _hudManager?.ClearFeedback();
            _hudManager?.UpdateHUD(actingPlayer);
            _hudManager?.ShowInfo($"{runtimeCard.SourceCard.DisplayName} played.");
            Debug.Log($"[PlayerInputController] Played {runtimeCard.SourceCard.DisplayName} on {target.type}.");
        }

        private string ResolveCurrentPlayerKey()
        {
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

        private void ApplyBoardVisual(CardRuntimeState runtimeCard, string actingPlayerKey, CardTarget target)
        {
            if (target.type != CardTargetType.Tile || runtimeCard?.SourceCard == null)
            {
                return;
            }

            if (_hexGrid == null)
            {
                _hexGrid = FindFirstObjectByType<HexGrid>();
            }

            HexTile tile = _hexGrid != null ? _hexGrid.GetTile(target.tile) : null;
            if (tile == null)
            {
                return;
            }

            if (runtimeCard.SourceCard is WorldEffectCardData)
            {
                tile.PlaceWorldEffect(actingPlayerKey);
            }
        }
    }
}
