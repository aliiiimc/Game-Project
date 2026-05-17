// Rabie: "Added AI movement and attack action generation using UnitManager legal board actions, while keeping existing card action generation."
using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Generates legal actions from a real game snapshot without mutating state.
    /// </summary>
    public sealed class LegalActionGenerator
    {
        private readonly ICardTargetValidator _targetValidator;
        private readonly LegalActionDiagnostics _diagnostics;

        public LegalActionGenerator(ICardTargetValidator targetValidator)
        {
            _targetValidator = targetValidator;
            _diagnostics = new LegalActionDiagnostics();
        }

        public LegalActionDiagnostics Diagnostics => _diagnostics;

        public List<ComputerAction> GenerateLegalActions(ComputerGameSnapshot snapshot)
        {
            List<ComputerAction> legalActions = new List<ComputerAction>();

            if (snapshot == null || snapshot.ActingPlayer == null || snapshot.HexGrid == null)
            {
                return legalActions;
            }

            _diagnostics.Reset();

            if (snapshot.CurrentPhase != GamePhase.Play)
            {
                legalActions.Add(ComputerAction.CreateEndTurnAction(snapshot.ActingPlayerKey));
                return legalActions;
            }

            CardValidationContext validationContext = new CardValidationContext
            {
                ActingPlayerKey = snapshot.ActingPlayerKey,
                OpponentPlayerKey = snapshot.OpponentPlayerKey,
                Board = snapshot.BoardReader
            };

            //Ali : évite une erreur si la main IA est absente/vide
            IReadOnlyList<CardRuntimeState> handCards = snapshot.HandCards;
            if (handCards != null && handCards.Count > 0)
            {
                for (int i = 0; i < handCards.Count; i++)
                {
                    CardRuntimeState runtimeCard = handCards[i];
                    if (runtimeCard?.SourceCard == null)
                    {
                        _diagnostics.RecordCandidate();
                        continue;
                    }

                    if (runtimeCard.SourceCard is SpellCardData)
                    {
                        TryAddSpellFortAction(legalActions, validationContext, snapshot, runtimeCard);
                        continue;
                    }

                    GenerateTilePlacementActions(legalActions, validationContext, snapshot, runtimeCard);
                }
            }

            GenerateUnitAttackActions(legalActions, snapshot);
            GenerateUnitMovementActions(legalActions, snapshot);

            if (legalActions.Count == 0)
            {
                legalActions.Add(ComputerAction.CreateEndTurnAction(snapshot.ActingPlayerKey));
            }

            return legalActions;
        }

        private void TryAddSpellFortAction(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard)
        {
            if (snapshot == null || runtimeCard?.SourceCard == null)
            {
                return;
            }

            if (legalActions == null || validationContext == null)
            {
                return;
            }

            if (!TryGetOpponentFortTile(snapshot, out AxialCoord fortTile))
            {
                return;
            }

            //Ali :enemy Fort targeting is only valid for damage spells in v1.
            if (!(runtimeCard.SourceCard is SpellCardData spellCard) || spellCard.effectType != SpellEffectType.Damage)
            {
                return;
            }

            CardTarget target = new CardTarget
            {
                type = CardTargetType.EnemyFort,
                targetPlayerId = snapshot.OpponentPlayerKey,
                targetEntityId = "fort",
                tile = fortTile


            };

            CardValidationResult result = _targetValidator.Validate(validationContext, runtimeCard, target);
            if (!result.IsValid)
            {
                _diagnostics.RecordCandidate();
                _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                return;
            }

            _diagnostics.RecordCandidate();
            bool isFortRaceMove = snapshot.ActingPlayer.fortHp <= 8 || snapshot.ActingPlayer.fortHp < snapshot.OpponentPlayer.fortHp;
            // Ali: a Fort race means the AI is under pressure and should value direct Fort damage more. It's used in ActionScoringSystem


            var action = new ComputerAction($"Play {runtimeCard.SourceCard.DisplayName} on enemy fort", ActionType.PlaySpellCard)
            {
                actingPlayerId = snapshot.ActingPlayerKey,
                sourceCard = runtimeCard,
                sourceCardName = runtimeCard.SourceCard.DisplayName,
                target = target,
                cost = 0, // Ali: playing a card is free; card cost is only used when buying.
                isGeneratedByLegalReader = true,
                isLegalAction = true,
                willDestroyEnemyFort = WouldDestroyEnemyFort(snapshot, runtimeCard),
                isDefensiveMove = isFortRaceMove,
                isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
            };

            legalActions.Add(action);
        }

        private void GenerateTilePlacementActions(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard)
        {
            if (snapshot?.HexGrid == null)
            {
                return;
            }
            if (legalActions == null || validationContext == null || runtimeCard?.SourceCard == null)
            {
                return;
            }


            for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
            {
                for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    CardTarget target = new CardTarget
                    {
                        type = CardTargetType.Tile,
                        tile = coord
                    };

                    CardValidationResult result = _targetValidator.Validate(validationContext, runtimeCard, target);
                    if (!result.IsValid)
                    {
                        _diagnostics.RecordCandidate();
                        _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                        continue;
                    }

                    _diagnostics.RecordCandidate();

                    //Ali: cache the card type so action setup can reuse it without repeating the type check.
                    bool isCharacterCard = runtimeCard.SourceCard is CharacterCardData;
                    
                    ActionType actionType = isCharacterCard
                        ? ActionType.PlayUnitCard
                        : ActionType.PlayWorldEffectCard;

                    var action = new ComputerAction($"Play {runtimeCard.SourceCard.DisplayName} on {coord}", actionType)
                    {
                        actingPlayerId = snapshot.ActingPlayerKey,
                        sourceCard = runtimeCard,
                        sourceCardName = runtimeCard.SourceCard.DisplayName,
                        target = target,
                        cost = 0, // Ali: playing a card is free; card cost is only used when buying.
                        isGeneratedByLegalReader = true,
                        isLegalAction = true,
                        isDefensiveMove = ShouldDefend(snapshot, col),
                        movesCloserToEnemyFort = ShouldPushForward(snapshot, col),
                        //Ali: hasSynergyOnBoard is for Small scoring bonus score for characters placed forward because they can create board pressure.
                        hasSynergyOnBoard = (isCharacterCard && ShouldPushForward(snapshot, col)) || (!isCharacterCard && ShouldDefend(snapshot, col)),
                        isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                        isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
                    };

                    legalActions.Add(action);
                }
            }
        }

        private void GenerateUnitMovementActions(
            List<ComputerAction> legalActions,
            ComputerGameSnapshot snapshot)
        {
            if (legalActions == null || snapshot?.HexGrid == null)
            {
                return;
            }

            UnitManager unitManager = Object.FindFirstObjectByType<UnitManager>();
            if (unitManager == null)
            {
                return;
            }

            List<Unit> units = unitManager.GetUnitsForOwner(snapshot.ActingPlayerKey);
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (unit == null || unit.currentTile == null)
                {
                    continue;
                }

                List<HexTile> destinationTiles = unitManager.GetLegalMoveTiles(unit);
                for (int tileIndex = 0; tileIndex < destinationTiles.Count; tileIndex++)
                {
                    HexTile destinationTile = destinationTiles[tileIndex];
                    if (destinationTile == null)
                    {
                        continue;
                    }

                    _diagnostics.RecordCandidate();

                    int destinationColumn = snapshot.HexGrid.AxialToOffsetColumn(destinationTile.coord);
                    bool movesForward = IsForwardMove(snapshot, unit, destinationTile);

                    var action = new ComputerAction(
                        $"Move {unit.name} to ({destinationTile.coord.q}, {destinationTile.coord.r})",
                        ActionType.MoveUnit)
                    {
                        actingPlayerId = snapshot.ActingPlayerKey,
                        actingUnit = unit,
                        destinationTile = destinationTile,
                        target = new CardTarget
                        {
                            type = CardTargetType.Tile,
                            tile = destinationTile.coord
                        },
                        isGeneratedByLegalReader = true,
                        isLegalAction = true,
                        isDefensiveMove = ShouldDefend(snapshot, destinationColumn),
                        movesCloserToEnemyFort = movesForward,
                        movesBackward = IsBackwardMove(snapshot, unit, destinationTile),
                        hasSynergyOnBoard = movesForward
                    };

                    legalActions.Add(action);
                }
            }
        }

        private void GenerateUnitAttackActions(
            List<ComputerAction> legalActions,
            ComputerGameSnapshot snapshot)
        {
            if (legalActions == null || snapshot?.HexGrid == null)
            {
                return;
            }

            UnitManager unitManager = Object.FindFirstObjectByType<UnitManager>();
            if (unitManager == null)
            {
                return;
            }

            List<Unit> units = unitManager.GetUnitsForOwner(snapshot.ActingPlayerKey);
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (unit == null || unit.currentTile == null)
                {
                    continue;
                }

                List<HexTile> targetTiles = unitManager.GetLegalAttackTargets(unit);
                for (int targetIndex = 0; targetIndex < targetTiles.Count; targetIndex++)
                {
                    HexTile targetTile = targetTiles[targetIndex];
                    if (targetTile == null)
                    {
                        continue;
                    }

                    if (targetTile.tileType != "unit" && targetTile.tileType != "fort")
                    {
                        continue;
                    }

                    _diagnostics.RecordCandidate();

                    Unit targetUnit = unitManager.FindUnitOnTile(targetTile);
                    ActionType actionType = targetTile.tileType == "fort"
                        ? ActionType.AttackFort
                        : ActionType.AttackUnit;

                    CardTargetType targetType = actionType == ActionType.AttackFort
                        ? CardTargetType.EnemyFort
                        : CardTargetType.EnemyUnit;

                    string targetName = actionType == ActionType.AttackFort
                        ? "enemy fort"
                        : targetUnit != null ? targetUnit.name : "enemy unit";

                    int targetColumn = snapshot.HexGrid.AxialToOffsetColumn(targetTile.coord);

                    var action = new ComputerAction(
                        $"Attack {targetName} with {unit.name}",
                        actionType)
                    {
                        actingPlayerId = snapshot.ActingPlayerKey,
                        actingUnit = unit,
                        targetTile = targetTile,
                        target = new CardTarget
                        {
                            type = targetType,
                            tile = targetTile.coord,
                            targetCard = targetUnit != null ? targetUnit.RuntimeCard : null,
                            targetPlayerId = targetTile.owner,
                            targetEntityId = actionType == ActionType.AttackFort ? "fort" : "unit"
                        },
                        isGeneratedByLegalReader = true,
                        isLegalAction = true,
                        willDestroyEnemyFort = actionType == ActionType.AttackFort
                            && snapshot.OpponentPlayer != null
                            && unit.attack >= snapshot.OpponentPlayer.fortHp,
                        isDefensiveMove = ShouldDefend(snapshot, targetColumn),
                        destroysEnemyUnit = targetUnit != null && unit.attack >= targetUnit.health,
                        survivesTrade = targetUnit == null || unit.health > targetUnit.attack
                    };

                    legalActions.Add(action);
                }
            }
        }


        // Ali: reusable validator checks the real Fort tile, so AI fort targets need the Fort coordinate.
        private static bool TryGetOpponentFortTile(ComputerGameSnapshot snapshot, out AxialCoord fortTile)
        {
            fortTile = default;

            if (snapshot?.HexGrid == null)
            {
                return false;
            }

            string opponentKey = snapshot.OpponentPlayerKey;

            for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
            {
                for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    HexTile tile = snapshot.HexGrid.GetTile(coord);

                    if (tile != null && tile.tileType == "fort" && tile.owner == opponentKey)
                    {
                        fortTile = coord;
                        return true;
                    }
                }
            }

            return false;
        }



        // Ali: lets the AI recognize direct lethal Fort damage before scoring actions.
        private static bool WouldDestroyEnemyFort(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard)
        {
            if (snapshot == null || snapshot.OpponentPlayer == null)
            {
                return false;
            }

            if (!(runtimeCard?.SourceCard is SpellCardData spellCard))
            {
                return false;
            }

            if (spellCard.effectType != SpellEffectType.Damage)
            {
                return false;
            }

            return spellCard.effectPower >= snapshot.OpponentPlayer.fortHp;
        }

        // Ali: defensive placement matters more when the AI Fort is low.
        private static bool ShouldDefend(ComputerGameSnapshot snapshot, int col)
        {
            if (snapshot?.ActingPlayer == null)
            {
                return false;
            }

            return snapshot.ActingPlayer.fortHp < 8 && IsDefensiveColumn(snapshot, col);
        }


        private static bool IsDefensiveColumn(ComputerGameSnapshot snapshot, int col)
        {
            // Rabie: horizontal board - player defends left, computer/enemy defends right.
            return snapshot.ActingPlayerKey == "enemy"
                ? col >= snapshot.HexGrid.gridWidth - 2
                : col <= 1;
        }


        // Ali: push forward when the AI Fort is not in danger.
        private static bool ShouldPushForward(ComputerGameSnapshot snapshot, int col)
        {
            if (snapshot?.ActingPlayer == null)
            {
                return false;
            }

            return snapshot.ActingPlayer.fortHp >= 8 && IsForwardColumn(snapshot, col);
        }

        private static bool IsForwardColumn(ComputerGameSnapshot snapshot, int col)
        {
            // Rabie: computer/red side moves left toward the player fort; player moves right.
            return snapshot.ActingPlayerKey == "enemy"
                ? col < snapshot.HexGrid.gridWidth - 2
                : col > 1;
        }

        private static bool IsForwardMove(ComputerGameSnapshot snapshot, Unit unit, HexTile destinationTile)
        {
            if (snapshot?.HexGrid == null || unit?.currentTile == null || destinationTile == null)
            {
                return false;
            }

            int startColumn = snapshot.HexGrid.AxialToOffsetColumn(unit.currentTile.coord);
            int destinationColumn = snapshot.HexGrid.AxialToOffsetColumn(destinationTile.coord);

            return snapshot.ActingPlayerKey == "enemy"
                ? destinationColumn < startColumn
                : destinationColumn > startColumn;
        }

        private static bool IsBackwardMove(ComputerGameSnapshot snapshot, Unit unit, HexTile destinationTile)
        {
            if (snapshot?.HexGrid == null || unit?.currentTile == null || destinationTile == null)
            {
                return false;
            }

            int startColumn = snapshot.HexGrid.AxialToOffsetColumn(unit.currentTile.coord);
            int destinationColumn = snapshot.HexGrid.AxialToOffsetColumn(destinationTile.coord);

            return snapshot.ActingPlayerKey == "enemy"
                ? destinationColumn > startColumn
                : destinationColumn < startColumn;
        }
    }
}
