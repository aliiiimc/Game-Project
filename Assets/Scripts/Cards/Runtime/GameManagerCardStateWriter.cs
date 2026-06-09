using UnityEngine;
using System.Collections.Generic;

public sealed class GameManagerCardStateWriter : MonoBehaviour, ICardStateWriter
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool autoResolveGameManager = true;
    [SerializeField] private bool logTransactions = true;
    [SerializeField] private string player1Key = "player";
    [SerializeField] private string player2Key = "enemy";
    [SerializeField] private HexGrid boardSource;//Ali : référence vers le board
    [SerializeField] private WorldEffectManager worldEffectManager;
    // [SerializeField] : variable privée dans le code, mais visible dans l’Inspector Unity.


    public string LastActingPlayerId { get; private set; }

    private void Awake()
    {
        if (autoResolveGameManager && gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        //Ali: Load the hexgrid variable
        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

    }

    public bool TrySpendCost(string playerId, int amount)
    {
        LastActingPlayerId = playerId;

        PlayerState playerState = ResolvePlayer(playerId);
        if (playerState == null)
        {
            LogTransaction($"TrySpendCost failed: unknown player '{playerId}'.");
            return false;
        }

        int cost = Mathf.Max(0, amount);
        if (playerState.money < cost)
        {
            LogTransaction($"TrySpendCost failed: player '{playerId}' has {playerState.money}, needs {cost}.");
            return false;
        }

        playerState.money -= cost;
        LogTransaction($"TrySpendCost success: player '{playerId}' paid {cost}. Remaining={playerState.money}.");
        return true;
    }

    public void AddRevenue(string playerId, int amount)
    {
        LastActingPlayerId = playerId;

        PlayerState playerState = ResolvePlayer(playerId);
        if (playerState == null)
        {
            LogTransaction($"AddRevenue failed: unknown player '{playerId}'.");
            return;
        }

        int add = Mathf.Max(0, amount);
        playerState.money += add;
        LogTransaction($"AddRevenue: player='{playerId}' amount={add} total={playerState.money}.");
    }

    public void MoveCardToZone(CardRuntimeState card, CardZone zone)
    {
        if (card == null)
        {
            return;
        }

        card.MoveToZone(zone);
        LogTransaction($"MoveCardToZone: {card.SourceCard.DisplayName} -> {zone}.");
    }

    public void ManifestCard(CardRuntimeState card, AxialCoord tile)
    {
        if (card == null)
        {
            return;
        }

        //Ali:

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

        HexTile targetTile = boardSource != null ? boardSource.GetTile(tile) : null;
        string owner = ResolveOwnerForManifestedCard();
        bool placementSucceeded = false;


        // Ali: keep board manifestation inside the writer so Character and World Effect cards follow the same pipeline path.
        // Ali: the writer is the runtime layer that applies the real board result of a card play, so Character and World Effect manifestation should both happen here.
        if (targetTile != null)
        {
            if (card.SourceCard is CharacterCardData characterCardData)
            {
                placementSucceeded = boardSource != null
                    && boardSource.SpawnUnitFromCard(targetTile, owner, card) != null;
                if (!placementSucceeded)
                {
                    LogTransaction(
                        $"ManifestCard character placement failed: owner='{owner}', tileType='{targetTile.tileType}', tileOwner='{targetTile.owner}'.");
                }
                else
                {
                    Unit unit = FindUnitForCard(card);
                    if (unit != null)
                    {
                        UnitManager unitManager = FindFirstObjectByType<UnitManager>();
                        if (unitManager != null)
                        {
                            unitManager.NotifyUnitSpawned(unit, characterCardData);
                        }
                    }
                    if (boardSource != null
                    && !boardSource.IsInPlayerDeploymentZone(tile, owner)
                    && BoardPlacementRules.CanPlaceCharacter(tile, owner, boardSource, characterCardData))
                    {
                        LogTransaction($"[SpecialTrigger][Camp] Character spawned using Camp override at {tile}.");
                    }
                }
            }
            else if (card.SourceCard is WorldEffectCardData worldEffectCard)
            {
                if (worldEffectManager == null)
                {
                    LogTransaction("ManifestCard failed: no WorldEffectManager found.");
                }
                else
                {
                    placementSucceeded = !targetTile.HasWorldEffect() && targetTile.tileType == "empty"
                        ? worldEffectManager.TryPlaceFromCard(targetTile, owner, card, out _)
                        : targetTile.HasWorldEffect() && !targetTile.HasUnitOccupant()
                            && worldEffectManager.TryReplace(targetTile, owner, card, out _);

                    if (!placementSucceeded)
                    {
                        LogTransaction(
                            $"ManifestCard world effect placement failed: owner='{owner}', tileType='{targetTile.tileType}', tileOwner='{targetTile.owner}'.");
                    }
                }

                if (placementSucceeded)
                {
                    ApplySpecialWorldEffectOnManifest(worldEffectCard, card, owner, targetTile);
                }
            }
        }
        else
        {
            LogTransaction($"ManifestCard failed: target tile '{tile}' was not found.");
        }

        if (placementSucceeded)
        {
            card.ManifestOnBoard(tile);
            LogTransaction($"ManifestCard: {card.SourceCard.DisplayName} at {tile}.");
            return;
        }

        LogTransaction($"ManifestCard aborted: {card.SourceCard.DisplayName} was not manifested on board.");
    }

    public void ApplyDamage(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ApplyDamage(safeAmount);
            int currentHp = boardUnit.RuntimeCard != null && boardUnit.RuntimeCard.CurrentHp.HasValue ? boardUnit.RuntimeCard.CurrentHp.Value : 0;
            NotifyHUD($"{card.SourceCard.DisplayName} took {safeAmount} damage. [HP: {currentHp}]");
            LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} realUnit amount={safeAmount}.");
            return;
        }

        WorldEffect boardWorldEffect = FindWorldEffectForCard(card);
        if (boardWorldEffect != null)
        {
            card.ApplyDamage(safeAmount);

            if (card.CurrentHp.HasValue)
            {
                boardWorldEffect.health = card.CurrentHp.Value;
            }
            else
            {
                boardWorldEffect.health = Mathf.Max(0, boardWorldEffect.health - safeAmount);
            }

            if (boardWorldEffect.health <= 0)
            {
                HexTile structureTile = boardWorldEffect.currentTile;
                card.MoveToZone(CardZone.Discard);

                if (structureTile != null)
                {
                    EnsureWorldEffectManager();
                    worldEffectManager?.Remove(structureTile);
                }

                NotifyHUD($"{card.SourceCard.DisplayName} took {safeAmount} damage and was destroyed! [HP: 0]");
                LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} structure destroyed by {safeAmount} damage.");
                return;
            }

            NotifyHUD($"{card.SourceCard.DisplayName} took {safeAmount} damage. [HP: {boardWorldEffect.health}]");
            LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} realStructure amount={safeAmount}, hp={boardWorldEffect.health}.");
            return;
        }

        card.ApplyDamage(safeAmount);
        int genericHp = card.CurrentHp.HasValue ? card.CurrentHp.Value : 0;
        NotifyHUD($"{card.SourceCard.DisplayName} took {safeAmount} damage. [HP: {genericHp}]");
        LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} amount={safeAmount}.");
    }

    public void ApplyHeal(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ApplyHeal(safeAmount);
            int currentHp = boardUnit.RuntimeCard != null && boardUnit.RuntimeCard.CurrentHp.HasValue ? boardUnit.RuntimeCard.CurrentHp.Value : 0;
            NotifyHUD($"{card.SourceCard.DisplayName} was healed for {safeAmount}. [HP: {currentHp}]");
            LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} realUnit amount={safeAmount}.");
            return;
        }

        card.ApplyHeal(safeAmount);
        int genericHp = card.CurrentHp.HasValue ? card.CurrentHp.Value : 0;
        NotifyHUD($"{card.SourceCard.DisplayName} was healed for {safeAmount}. [HP: {genericHp}]");
        LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} amount={safeAmount}.");
    }

    //Ali:
    public void ApplyFortDamage(string playerId, int amount)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);// On prend la plus grande valeur entre 0 et amount, On veut empêcher des valeurs négatives de passer.

        if (safeAmount <= 0)
        {
            return;
        }

        if (playerId == player1Key || (gameManager.player1 != null && playerId == gameManager.player1.playerName))
        {
            gameManager.DamagePlayer1Fort(safeAmount);
            string pName = gameManager.player1 != null ? gameManager.player1.playerName : "Player 1";
            int fHp = gameManager.player1 != null ? gameManager.player1.fortHp : 0;
            NotifyHUD($"{pName}'s Fort took {safeAmount} damage. [HP: {fHp}]");
            LogTransaction($"ApplyFortDamage: player='{playerId}' amount={safeAmount}.");
            return;
        }

        if (playerId == player2Key || (gameManager.player2 != null && playerId == gameManager.player2.playerName))
        {
            gameManager.DamagePlayer2Fort(safeAmount);
            string pName = gameManager.player2 != null ? gameManager.player2.playerName : "Player 2";
            int fHp = gameManager.player2 != null ? gameManager.player2.fortHp : 0;
            NotifyHUD($"{pName}'s Fort took {safeAmount} damage. [HP: {fHp}]");
            LogTransaction($"ApplyFortDamage: player='{playerId}' amount={safeAmount}.");
        }
    }

    //Ali:
    public void ApplyFortHeal(string playerId, int amount)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        if (playerId == player1Key || (gameManager.player1 != null && playerId == gameManager.player1.playerName))
        {
            gameManager.HealPlayer1Fort(safeAmount);
            string pName = gameManager.player1 != null ? gameManager.player1.playerName : "Player 1";
            int fHp = gameManager.player1 != null ? gameManager.player1.fortHp : 0;
            NotifyHUD($"{pName}'s Fort was healed for {safeAmount}. [HP: {fHp}]");
            LogTransaction($"ApplyFortHeal: player='{playerId}' amount={safeAmount}.");
            return;
        }

        if (playerId == player2Key || (gameManager.player2 != null && playerId == gameManager.player2.playerName))
        {
            gameManager.HealPlayer2Fort(safeAmount);
            string pName = gameManager.player2 != null ? gameManager.player2.playerName : "Player 2";
            int fHp = gameManager.player2 != null ? gameManager.player2.fortHp : 0;
            NotifyHUD($"{pName}'s Fort was healed for {safeAmount}. [HP: {fHp}]");
            LogTransaction($"ApplyFortHeal: player='{playerId}' amount={safeAmount}.");
        }
    }


    public void ModifyDamage(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ModifyAttack(delta);
            LogTransaction($"ModifyDamage: {card.SourceCard.DisplayName} realUnit delta={delta}.");
            return;
        }

        card.ModifyDamage(delta);
        LogTransaction($"ModifyDamage: {card.SourceCard.DisplayName} delta={delta}.");
    }

    public void ModifyMovement(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ModifyMovementRange(delta);
            LogTransaction($"ModifyMovement: {card.SourceCard.DisplayName} realUnit delta={delta}.");
            return;
        }

        card.ModifyMovement(delta);
        LogTransaction($"ModifyMovement: {card.SourceCard.DisplayName} delta={delta}.");
    }

    public int GetMoney(string playerId)
    {
        PlayerState playerState = ResolvePlayer(playerId);
        return playerState != null ? playerState.money : 0;
    }

    private PlayerState ResolvePlayer(string playerId)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return null;
        }

        if (gameManager.player1 != null && (playerId == player1Key || playerId == gameManager.player1.playerName))
        {
            return gameManager.player1;
        }

        if (gameManager.player2 != null && (playerId == player2Key || playerId == gameManager.player2.playerName))
        {
            return gameManager.player2;
        }

        if (gameManager.currentPlayer != null && playerId == "current")
        {
            return gameManager.currentPlayer;
        }

        return null;
    }

    private string ResolveOwnerForManifestedCard()
    {
        if (LastActingPlayerId == PlayerKeyResolver.PlayerTwoKey || LastActingPlayerId == player2Key)
        {
            return PlayerKeyResolver.PlayerTwoKey;
        }

        if (LastActingPlayerId == PlayerKeyResolver.PlayerOneKey || LastActingPlayerId == player1Key)
        {
            return PlayerKeyResolver.PlayerOneKey;
        }

        if (gameManager != null && gameManager.currentPlayer != null)
        {
            if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
            {
                return PlayerKeyResolver.PlayerTwoKey;
            }

            if (ReferenceEquals(gameManager.currentPlayer, gameManager.player1))
            {
                return PlayerKeyResolver.PlayerOneKey;
            }
        }

        return PlayerKeyResolver.PlayerOneKey;
    }

    private Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && ReferenceEquals(unit.RuntimeCard, card))
            {
                return unit;
            }
        }

        return null;
    }

    private WorldEffect FindWorldEffectForCard(CardRuntimeState card)
    {
        if (card == null || !card.IsManifestedOnBoard)
        {
            return null;
        }

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }

        if (boardSource == null)
        {
            return null;
        }

        EnsureWorldEffectManager();

        HexTile targetTile = boardSource.GetTile(card.BoardPosition);
        if (targetTile == null)
        {
            return null;
        }

        WorldEffect worldEffect = worldEffectManager != null
            ? worldEffectManager.FindWorldEffectOnTile(targetTile)
            : null;

        return worldEffect != null && ReferenceEquals(worldEffect.sourceCard, card)
            ? worldEffect
            : null;
    }

    private void EnsureWorldEffectManager()
    {
        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }

        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }
    }

    private void LogTransaction(string message)
    {
        if (logTransactions)
        {
            Debug.Log($"[GameManagerCardStateWriter] {message}");
        }
    }

    private void ApplySpecialWorldEffectOnManifest(WorldEffectCardData worldEffectCard, CardRuntimeState runtimeCard, string owner, HexTile targetTile)
    {
        if (worldEffectCard == null || runtimeCard == null || targetTile == null)
        {
            return;
        }

        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

        // Reset card-specific tile metadata before assigning the new world-effect behavior.
        if (worldEffectManager != null)
        {
            worldEffectManager.TryClearSpecialData(targetTile);
        }

        if (worldEffectCard is WheatFieldCardData)
        {
            WheatField wheatField = new WheatField();
            if (!wheatField.ApplyFieldCluster(
                boardSource,
                targetTile,
                owner,
                worldEffectCard.manifestedSprite,
                runtimeCard,
                out string clusterId))
            {
                LogTransaction("Wheat field cluster creation failed.");
            }
            else
            {
                LogTransaction($"Wheat field cluster created: {clusterId}.");
            }

            return;
        }

        if (worldEffectCard is MinesCardData minesCardData)
        {
            Mines mines = new Mines();
            int placedMineCount = mines.ApplyMinefield(boardSource, worldEffectManager, minesCardData, runtimeCard, owner, targetTile);
            LogTransaction($"{mines.GetEnemyWarningMessage()} Placed {placedMineCount} mine(s).");

            if (gameManager != null)
            {
                gameManager.UpdateMineVisibilityForBoardViewer();
                if (owner == PlayerKeyResolver.PlayerTwoKey)
                {
                    gameManager.NotifyEnemyMinefieldPlaced(placedMineCount);
                }
            }
            return;
        }

        if (worldEffectCard is CampCardData campCardData)
        {
            Camp camp = new Camp();
            if (camp.TryActivateOnTile(worldEffectManager, targetTile, campCardData))
            {
                LogTransaction("Camp world effect activated on tile.");
            }
            else
            {
                LogTransaction("Camp world effect activation failed.");
            }

            return;
        }

        if (worldEffectCard is WallCardData)
        {
            Wall wall = new Wall();
            if (!wall.ApplyWallLine(boardSource, targetTile, owner, runtimeCard))
            {
                LogTransaction("Wall line creation failed.");
            }
            else
            {
                LogTransaction("Wall line created.");
            }
        }
    }

    private WorldEffectManager CreateWorldEffectManagerFallback()
    {
        GameObject managerObject = new GameObject("WorldEffectManager");
        WorldEffectManager manager = managerObject.AddComponent<WorldEffectManager>();
        Debug.LogWarning("[GameManagerCardStateWriter] WorldEffectManager was missing in scene. Created runtime fallback.");
        return manager;
    }

    private void NotifyHUD(string message)
    {
        var hud = FindFirstObjectByType<FortGame.UI.HUDManager>();
        if (hud != null)
        {
            hud.ShowSpellAnnouncement(message);
        }
    }
}
