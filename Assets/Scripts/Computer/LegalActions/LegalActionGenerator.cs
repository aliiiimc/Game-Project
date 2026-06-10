// Rabie: "Added tactical scoring for attack targets and move-to-attack threats so AI choices look more intentional."
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
                        GenerateSpellActions(legalActions, validationContext, snapshot, runtimeCard);
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

        private void GenerateSpellActions(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard)
        {
            if (snapshot == null || !(runtimeCard?.SourceCard is SpellCardData spellCard))
            {
                return;
            }

            if (legalActions == null || validationContext == null)
            {
                return;
            }

            if (spellCard.MatchesSpecialCard(SpecialCardIds.SpellRevival, "Revival"))
            {
                GenerateRevivalActions(legalActions, validationContext, snapshot, runtimeCard, spellCard);
                return;
            }

            for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
            {
                for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    HexTile tile = snapshot.HexGrid.GetTile(coord);
                    if (!TryBuildSpellTargetFromTile(snapshot, runtimeCard, tile, out CardTarget target))
                    {
                        continue;
                    }

                    TryAddSpellAction(legalActions, validationContext, snapshot, runtimeCard, target);
                }
            }
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
                    bool developsBoard = isCharacterCard && ShouldDevelopBoard(snapshot, col);

                    float tacticalScore = ScorePlacementTile(snapshot, coord, isCharacterCard);
                    if (runtimeCard.SourceCard is UfoCowCardData)
                    {
                        tacticalScore += ScoreUfoCowPlacement(snapshot, coord);
                    }

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
                        movesCloserToEnemyFort = ShouldPushForward(snapshot, col) || developsBoard,
                        //Ali: hasSynergyOnBoard is for Small scoring bonus score for characters placed forward because they can create board pressure.
                        hasSynergyOnBoard = (isCharacterCard && (ShouldPushForward(snapshot, col) || developsBoard)) || (!isCharacterCard && ShouldDefend(snapshot, col)),
                        // (abdo :) Spawn cards get a tile score so the AI spreads pressure instead of picking the first valid row.
                        tacticalScore = tacticalScore,
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
                    int movementDistance = HexUtils.GetMoveDistance(
                        unit.currentTile,
                        destinationTile,
                        snapshot.HexGrid,
                        unit.GetRemainingMovement(),
                        unit.sourceCharacterCardData != null ? unit.sourceCharacterCardData.movementType : MovementType.Ground,
                        unit.owner);

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
                        hasSynergyOnBoard = movesForward,
                        // (abdo :) Movement gets scored by forward progress and distance so the AI uses its range instead of tiny steps.
                        tacticalScore = ScoreMoveDestination(snapshot, unit, destinationTile, movementDistance, unitManager)
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

                    bool isWorldEffect = targetTile.HasWorldEffect() && targetTile.tileType != "unit" && targetTile.tileType != "fort";
                    if (targetTile.tileType != "unit" && targetTile.tileType != "fort" && !isWorldEffect)
                    {
                        continue;
                    }

                    if (isWorldEffect && targetTile.isMineTile)
                    {
                        continue;
                    }

                    _diagnostics.RecordCandidate();

                    Unit targetUnit = unitManager.FindUnitOnTile(targetTile);
                    CardRuntimeState targetWorldEffect = null;
                    if (isWorldEffect)
                    {
                        targetWorldEffect = snapshot.BoardReader != null ? snapshot.BoardReader.GetCardAt(targetTile.coord) : null;
                    }

                    ActionType actionType = targetTile.tileType == "fort"
                        ? ActionType.AttackFort
                        : (isWorldEffect ? ActionType.AttackStructure : ActionType.AttackUnit);

                    CardTargetType targetType = actionType == ActionType.AttackFort
                        ? CardTargetType.EnemyFort
                        : (actionType == ActionType.AttackStructure ? CardTargetType.EnemyStructure : CardTargetType.EnemyUnit);

                    string targetName = actionType == ActionType.AttackFort
                        ? "enemy fort"
                        : (actionType == ActionType.AttackStructure ? (targetWorldEffect?.SourceCard?.DisplayName ?? "enemy structure") : (targetUnit != null ? targetUnit.name : "enemy unit"));

                    string targetEntityId = actionType == ActionType.AttackFort ? "fort" : (actionType == ActionType.AttackStructure ? "worldEffect" : "unit");
                    string targetOwner = actionType == ActionType.AttackStructure ? targetTile.worldEffectOwner : targetTile.owner;
                    CardRuntimeState targetCard = actionType == ActionType.AttackStructure ? targetWorldEffect : (targetUnit != null ? targetUnit.RuntimeCard : null);

                    int targetColumn = snapshot.HexGrid.AxialToOffsetColumn(targetTile.coord);

                    bool destroysEnemyUnit = actionType == ActionType.AttackStructure
                        ? (targetWorldEffect?.SourceCard is WorldEffectCardData we && unit.attack >= (we.structureHp.HasValue ? we.structureHp.Value : 1))
                        : (targetUnit != null && unit.attack >= targetUnit.health);

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
                            targetCard = targetCard,
                            targetPlayerId = targetOwner,
                            targetEntityId = targetEntityId
                        },
                        isGeneratedByLegalReader = true,
                        isLegalAction = true,
                        willDestroyEnemyFort = actionType == ActionType.AttackFort
                            && snapshot.OpponentPlayer != null
                            && unit.attack >= snapshot.OpponentPlayer.fortHp,
                        isDefensiveMove = ShouldDefend(snapshot, targetColumn),
                        destroysEnemyUnit = destroysEnemyUnit,
                        survivesTrade = targetUnit == null || unit.health > targetUnit.attack,
                        tacticalScore = ScoreAttackTarget(snapshot, unit, targetTile, targetUnit, actionType)
                    };

                    legalActions.Add(action);
                }
            }
        }

        private static float ScorePlacementTile(ComputerGameSnapshot snapshot, AxialCoord coord, bool isCharacterCard)
        {
            // (abdo :) Prefer a useful central band, then add lane spread and a tiny stable tie-breaker.
            if (snapshot?.HexGrid == null)
            {
                return 0f;
            }

            int col = snapshot.HexGrid.AxialToOffsetColumn(coord);
            float score = GetCenterBandScore(snapshot, coord.r) * 18f;
            score += ScoreLaneSpread(snapshot, coord);
            score += GetStableTileJitter(coord) * 6f;

            if (isCharacterCard)
            {
                int preferredFrontColumn = snapshot.ActingPlayerKey == "enemy"
                    ? snapshot.HexGrid.gridWidth - 2
                    : 1;

                score += Mathf.Max(0f, 18f - Mathf.Abs(col - preferredFrontColumn) * 18f);
            }
            else if (ShouldDefend(snapshot, col))
            {
                score += 12f;
            }

            return score;
        }

        private static float ScoreMoveDestination(ComputerGameSnapshot snapshot, Unit unit, HexTile destinationTile, int movementDistance, UnitManager unitManager)
        {
            // (abdo :) Reward forward movement and spending movement budget, while still keeping lanes near the middle useful.
            if (snapshot?.HexGrid == null || unit?.currentTile == null || destinationTile == null)
            {
                return 0f;
            }

            int forwardProgress = GetForwardProgress(snapshot, unit.currentTile, destinationTile);
            int safeDistance = Mathf.Max(0, movementDistance);

            float score = 0f;
            score += Mathf.Max(0, forwardProgress) * 45f;
            score += safeDistance * 12f;
            score += GetCenterRowScore(snapshot, destinationTile.coord.r) * 18f;
            score += ScoreMoveToAttackThreat(snapshot, unit, destinationTile, safeDistance, unitManager);

            if (forwardProgress <= 0)
            {
                int col = snapshot.HexGrid.AxialToOffsetColumn(destinationTile.coord);
                if (!ShouldDefend(snapshot, col))
                {
                    score -= 10f;
                }
            }

            return score;
        }

        private void GenerateRevivalActions(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard,
            SpellCardData spellCard)
        {
            int lookbackTurns = Mathf.Max(0, spellCard.effectDurationTurns);
            if (lookbackTurns <= 0)
            {
                return;
            }

            List<CharacterCardData> choices = DeathHistoryManager.GetOrCreate().GetRecentCharacterChoices(lookbackTurns);
            if (choices == null || choices.Count == 0)
            {
                return;
            }

            for (int choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
            {
                CharacterCardData revivedCharacter = choices[choiceIndex];
                if (revivedCharacter == null)
                {
                    continue;
                }

                for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
                {
                    for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                    {
                        AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                        CardRuntimeState revivedRuntime = CardFactory.CreateRuntimeState(revivedCharacter);
                        CardTarget target = new CardTarget
                        {
                            type = CardTargetType.Tile,
                            tile = coord
                        };

                        CardValidationResult result = _targetValidator.Validate(validationContext, revivedRuntime, target);
                        if (!result.IsValid)
                        {
                            _diagnostics.RecordCandidate();
                            _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                            continue;
                        }

                        _diagnostics.RecordCandidate();
                        float tacticalScore = ScoreRevivalPlacement(snapshot, revivedRuntime, coord);
                        legalActions.Add(new ComputerAction($"Play Revival to place {revivedCharacter.DisplayName} on {coord}", ActionType.PlaySpellCard)
                        {
                            actingPlayerId = snapshot.ActingPlayerKey,
                            sourceCard = runtimeCard,
                            auxiliaryCard = revivedRuntime,
                            sourceCardName = runtimeCard.SourceCard.DisplayName,
                            target = target,
                            cost = 0,
                            isGeneratedByLegalReader = true,
                            isLegalAction = true,
                            hasSynergyOnBoard = true,
                            movesCloserToEnemyFort = ShouldPushForward(snapshot, snapshot.HexGrid.AxialToOffsetColumn(coord)),
                            tacticalScore = tacticalScore,
                            isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                            isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
                        });
                    }
                }
            }
        }

        private void TryAddSpellAction(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard,
            CardTarget target)
        {
            if (legalActions == null || validationContext == null || snapshot == null || runtimeCard?.SourceCard == null)
            {
                return;
            }

            CardValidationResult result = ValidateSpellTarget(validationContext, snapshot, runtimeCard, target);
            if (!result.IsValid)
            {
                _diagnostics.RecordCandidate();
                _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                return;
            }

            _diagnostics.RecordCandidate();

            int targetColumn = snapshot.HexGrid.AxialToOffsetColumn(target.tile);
            float tacticalScore = ScoreSpellTarget(snapshot, runtimeCard, target);
            bool isDefensiveMove = tacticalScore >= 180f || ShouldDefend(snapshot, targetColumn);
            bool destroysEnemyUnit = target.type == CardTargetType.EnemyUnit
                && target.targetCard != null
                && GetSpellDamageAmount(runtimeCard) >= GetCardCurrentHp(target.targetCard);

            legalActions.Add(new ComputerAction($"Play {runtimeCard.SourceCard.DisplayName} on {DescribeSpellTarget(target)}", ActionType.PlaySpellCard)
            {
                actingPlayerId = snapshot.ActingPlayerKey,
                sourceCard = runtimeCard,
                sourceCardName = runtimeCard.SourceCard.DisplayName,
                target = target,
                cost = 0,
                isGeneratedByLegalReader = true,
                isLegalAction = true,
                willDestroyEnemyFort = target.type == CardTargetType.EnemyFort && WouldDestroyEnemyFort(snapshot, runtimeCard),
                isDefensiveMove = isDefensiveMove,
                destroysEnemyUnit = destroysEnemyUnit,
                survivesTrade = true,
                hasSynergyOnBoard = tacticalScore >= 120f,
                tacticalScore = tacticalScore,
                isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
            });
        }

        private CardValidationResult ValidateSpellTarget(
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard,
            CardTarget target)
        {
            CardPlayService cardPlayService = ResolveCardPlayService(snapshot);
            if (cardPlayService != null)
            {
                CardPlayResult playResult = cardPlayService.CanPlayCard(runtimeCard, snapshot.ActingPlayerKey, target);
                if (playResult.Succeeded)
                {
                    return CardValidationResult.Valid();
                }

                return CardValidationResult.Invalid(playResult.ReasonCode, playResult.Message);
            }

            return _targetValidator.Validate(validationContext, runtimeCard, target);
        }

        private static CardPlayService ResolveCardPlayService(ComputerGameSnapshot snapshot)
        {
            if (snapshot?.GameManager != null)
            {
                CardPlayService serviceOnGameManager = snapshot.GameManager.GetComponent<CardPlayService>();
                if (serviceOnGameManager != null)
                {
                    return serviceOnGameManager;
                }
            }

            return Object.FindFirstObjectByType<CardPlayService>();
        }

        private static bool TryBuildSpellTargetFromTile(
            ComputerGameSnapshot snapshot,
            CardRuntimeState sourceCard,
            HexTile tile,
            out CardTarget target)
        {
            target = default;

            if (snapshot == null || sourceCard?.SourceCard == null || tile == null)
            {
                return false;
            }

            AxialCoord coord = tile.coord;
            if (tile.tileType == "fort")
            {
                bool isAllyFort = tile.owner == snapshot.ActingPlayerKey;
                target = new CardTarget
                {
                    type = isAllyFort ? CardTargetType.AllyFort : CardTargetType.EnemyFort,
                    tile = coord,
                    targetPlayerId = tile.owner,
                    targetEntityId = "fort"
                };
                return true;
            }

            CardRuntimeState runtimeTarget = snapshot.BoardReader != null ? snapshot.BoardReader.GetCardAt(coord) : null;
            if (runtimeTarget == null || runtimeTarget.SourceCard == null)
            {
                return false;
            }

            if (runtimeTarget.SourceCard is CharacterCardData)
            {
                bool isAllyUnit = tile.owner == snapshot.ActingPlayerKey;
                target = new CardTarget
                {
                    type = isAllyUnit ? CardTargetType.AllyUnit : CardTargetType.EnemyUnit,
                    tile = coord,
                    targetCard = runtimeTarget,
                    targetPlayerId = tile.owner,
                    targetEntityId = "unit"
                };
                return true;
            }

            if (runtimeTarget.SourceCard is WorldEffectCardData)
            {
                bool isAllyStructure = tile.worldEffectOwner == snapshot.ActingPlayerKey;
                target = new CardTarget
                {
                    type = isAllyStructure ? CardTargetType.AllyStructure : CardTargetType.EnemyStructure,
                    tile = coord,
                    targetCard = runtimeTarget,
                    targetPlayerId = tile.worldEffectOwner,
                    targetEntityId = "worldEffect"
                };
                return true;
            }

            return false;
        }

        private static string DescribeSpellTarget(CardTarget target)
        {
            switch (target.type)
            {
                case CardTargetType.AllyFort:
                    return "ally fort";
                case CardTargetType.EnemyFort:
                    return "enemy fort";
                case CardTargetType.AllyUnit:
                case CardTargetType.EnemyUnit:
                case CardTargetType.AllyStructure:
                case CardTargetType.EnemyStructure:
                    return target.targetCard != null && target.targetCard.SourceCard != null
                        ? target.targetCard.SourceCard.DisplayName
                        : target.type.ToString();
                default:
                    return target.type.ToString();
            }
        }

        private static float ScoreSpellTarget(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard, CardTarget target)
        {
            if (!(runtimeCard?.SourceCard is SpellCardData spellCard) || snapshot == null)
            {
                return 0f;
            }

            if (spellCard.MatchesSpecialCard(SpecialCardIds.SpellSabotage, "Sabotage"))
            {
                return ScoreStructureDisruption(snapshot, target, 220f);
            }

            if (spellCard.MatchesSpecialCard(SpecialCardIds.SpellTaxCollection, "Tax collection"))
            {
                return ScoreStructureDisruption(snapshot, target, 180f);
            }

            switch (spellCard.effectType)
            {
                case SpellEffectType.Damage:
                    return ScoreDamageSpell(snapshot, runtimeCard, target);
                case SpellEffectType.Heal:
                    return ScoreHealSpell(snapshot, runtimeCard, target);
                case SpellEffectType.Buff:
                case SpellEffectType.Boost:
                case SpellEffectType.Utility:
                    return ScoreSupportSpell(snapshot, runtimeCard, target);
                case SpellEffectType.Debuff:
                    return ScoreDebuffSpell(snapshot, runtimeCard, target);
                default:
                    return 0f;
            }
        }

        private static float ScoreDamageSpell(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard, CardTarget target)
        {
            int damage = GetSpellDamageAmount(runtimeCard, target);
            if (target.type == CardTargetType.EnemyFort)
            {
                int opponentFortHp = snapshot.OpponentPlayer != null ? Mathf.Max(1, snapshot.OpponentPlayer.fortHp) : 1;
                if (damage >= opponentFortHp)
                {
                    return 10000f;
                }

                float score = 140f + damage * 28f;
                if (opponentFortHp <= 8)
                {
                    score += 150f;
                }

                return score;
            }

            if (target.type == CardTargetType.EnemyUnit && target.targetCard != null)
            {
                int hp = GetCardCurrentHp(target.targetCard);
                int attack = GetCardCurrentDamage(target.targetCard);
                float score = 90f + attack * 22f + ScoreThreatNearOwnFort(snapshot, target.tile) + Mathf.Min(120f, damage * 20f);
                if (damage >= hp)
                {
                    score += 220f;
                }

                return score;
            }

            if (target.type == CardTargetType.EnemyStructure)
            {
                return ScoreStructureDisruption(snapshot, target, 170f) + damage * 18f;
            }

            return 0f;
        }

        private static float ScoreHealSpell(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard, CardTarget target)
        {
            int healAmount = GetSpellHealAmount(runtimeCard);
            if (healAmount <= 0)
            {
                return -50f;
            }

            if (target.type == CardTargetType.AllyFort && snapshot.ActingPlayer != null)
            {
                int maxFortHp = snapshot.GameManager != null && snapshot.GameManager.gameConfig != null
                    ? Mathf.Max(1, snapshot.GameManager.gameConfig.startingFortHp)
                    : Mathf.Max(1, snapshot.ActingPlayer.fortHp);
                int missingHp = Mathf.Max(0, maxFortHp - snapshot.ActingPlayer.fortHp);
                if (missingHp <= 0)
                {
                    return -200f;
                }

                float score = 120f + Mathf.Min(missingHp, healAmount) * 24f;
                if (snapshot.ActingPlayer.fortHp <= 8)
                {
                    score += 180f;
                }

                return score;
            }

            if (target.type == CardTargetType.AllyUnit && target.targetCard != null)
            {
                int missingHp = GetCardMissingHp(target.targetCard);
                if (missingHp <= 0)
                {
                    return -200f;
                }

                return 110f + Mathf.Min(missingHp, healAmount) * 22f + GetCardCurrentDamage(target.targetCard) * 14f;
            }

            return 0f;
        }

        private static float ScoreSupportSpell(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard, CardTarget target)
        {
            if (target.type != CardTargetType.AllyUnit || target.targetCard == null)
            {
                return -80f;
            }

            float score = 100f + GetCardCurrentDamage(target.targetCard) * 18f;
            Unit targetUnit = FindUnitForCard(target.targetCard);
            if (targetUnit != null && targetUnit.CanAttack())
            {
                score += 90f;
            }

            score += Mathf.Max(0f, ScoreThreatNearEnemyFort(snapshot, target.tile));
            return score;
        }

        private static float ScoreDebuffSpell(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard, CardTarget target)
        {
            if (target.type != CardTargetType.EnemyUnit || target.targetCard == null)
            {
                return -80f;
            }

            float score = 130f + GetCardCurrentDamage(target.targetCard) * 20f + ScoreThreatNearOwnFort(snapshot, target.tile);
            if (runtimeCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellFreeze, "Freeze"))
            {
                score += 80f;
            }

            Unit targetUnit = FindUnitForCard(target.targetCard);
            if (targetUnit != null && targetUnit.CanAttack())
            {
                score += 70f;
            }

            return score;
        }

        private static float ScoreStructureDisruption(ComputerGameSnapshot snapshot, CardTarget target, float baseScore)
        {
            if (target.targetCard == null)
            {
                return 0f;
            }

            float score = baseScore;
            if (target.targetCard.CurrentRevenue.HasValue)
            {
                score += target.targetCard.CurrentRevenue.Value * 45f;
            }

            int hp = GetCardCurrentHp(target.targetCard);
            if (hp > 0)
            {
                score += hp * 10f;
            }

            score += ScoreThreatNearOwnFort(snapshot, target.tile) * 0.35f;
            return score;
        }

        private static float ScoreRevivalPlacement(ComputerGameSnapshot snapshot, CardRuntimeState revivedRuntime, AxialCoord coord)
        {
            bool isCharacterCard = revivedRuntime != null && revivedRuntime.SourceCard is CharacterCardData;
            float placementScore = ScorePlacementTile(snapshot, coord, isCharacterCard);
            float bodyScore = revivedRuntime != null ? GetCardCurrentDamage(revivedRuntime) * 18f + GetCardCurrentHp(revivedRuntime) * 6f : 0f;
            return 180f + placementScore + bodyScore;
        }

        private static int GetSpellDamageAmount(CardRuntimeState runtimeCard)
        {
            return GetSpellDamageAmount(runtimeCard, default);
        }

        private static int GetSpellDamageAmount(CardRuntimeState runtimeCard, CardTarget target)
        {
            if (runtimeCard == null)
            {
                return 0;
            }

            if (runtimeCard.SourceCard != null && runtimeCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellSabotage, "Sabotage"))
            {
                return 0;
            }

            if (runtimeCard.SourceCard != null && runtimeCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellTaxCollection, "Tax collection"))
            {
                return 0;
            }

            if (target.type == CardTargetType.EnemyFort
                && runtimeCard.SourceCard != null
                && runtimeCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellLightningStrike, "Lightning Strike"))
            {
                return LightningStrike.FortDamageAmount;
            }

            return runtimeCard.SpellEffectPower.HasValue ? Mathf.Max(0, runtimeCard.SpellEffectPower.Value) : 0;
        }

        private static int GetSpellHealAmount(CardRuntimeState runtimeCard)
        {
            return runtimeCard != null && runtimeCard.SpellEffectPower.HasValue
                ? Mathf.Max(0, runtimeCard.SpellEffectPower.Value)
                : 0;
        }

        private static int GetCardCurrentHp(CardRuntimeState card)
        {
            if (card == null)
            {
                return 0;
            }

            if (card.CurrentHp.HasValue)
            {
                return Mathf.Max(0, card.CurrentHp.Value);
            }

            if (card.SourceCard is CharacterCardData characterCard)
            {
                return Mathf.Max(0, characterCard.maxHp);
            }

            if (card.SourceCard is WorldEffectCardData worldEffectCard && worldEffectCard.structureHp.HasValue)
            {
                return Mathf.Max(0, worldEffectCard.structureHp.Value);
            }

            return 0;
        }

        private static int GetCardCurrentDamage(CardRuntimeState card)
        {
            if (card == null)
            {
                return 0;
            }

            if (card.CurrentDamage.HasValue)
            {
                return Mathf.Max(0, card.CurrentDamage.Value);
            }

            if (card.SourceCard is CharacterCardData characterCard)
            {
                return Mathf.Max(0, characterCard.attackDamage);
            }

            if (card.SourceCard is WorldEffectCardData worldEffectCard && worldEffectCard.structureDamage.HasValue)
            {
                return Mathf.Max(0, worldEffectCard.structureDamage.Value);
            }

            return 0;
        }

        private static int GetCardMissingHp(CardRuntimeState card)
        {
            if (card == null)
            {
                return 0;
            }

            int currentHp = GetCardCurrentHp(card);
            int maxHp = 0;

            if (card.SourceCard is CharacterCardData characterCard)
            {
                maxHp = Mathf.Max(0, characterCard.maxHp);
            }
            else if (card.SourceCard is WorldEffectCardData worldEffectCard && worldEffectCard.structureHp.HasValue)
            {
                maxHp = Mathf.Max(0, worldEffectCard.structureHp.Value);
            }

            return Mathf.Max(0, maxHp - currentHp);
        }

        private static Unit FindUnitForCard(CardRuntimeState runtimeCard)
        {
            if (runtimeCard == null)
            {
                return null;
            }

            Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            for (int i = 0; i < units.Length; i++)
            {
                Unit unit = units[i];
                if (unit != null && ReferenceEquals(unit.RuntimeCard, runtimeCard))
                {
                    return unit;
                }
            }

            return null;
        }

        private static float ScoreThreatNearEnemyFort(ComputerGameSnapshot snapshot, AxialCoord targetCoord)
        {
            if (!TryGetFortTile(snapshot, snapshot?.OpponentPlayerKey, out AxialCoord fortCoord))
            {
                return 0f;
            }

            int distance = HexUtils.GetAxialDistance(targetCoord, fortCoord);
            return Mathf.Max(0f, 6 - distance) * 18f;
        }

        private static float ScoreThreatNearOwnFort(ComputerGameSnapshot snapshot, AxialCoord targetCoord)
        {
            if (snapshot?.HexGrid == null)
            {
                return 0f;
            }

            HexTile targetTile = snapshot.HexGrid.GetTile(targetCoord);
            return ScoreThreatNearOwnFort(snapshot, targetTile);
        }

        private static float ScoreAttackTarget(ComputerGameSnapshot snapshot, Unit attacker, HexTile targetTile, Unit targetUnit, ActionType actionType)
        {
            if (snapshot?.HexGrid == null || attacker == null || targetTile == null)
            {
                return 0f;
            }

            bool isAlly = false;
            if (actionType == ActionType.AttackFort)
            {
                isAlly = targetTile.owner == snapshot.ActingPlayerKey;
            }
            else if (actionType == ActionType.AttackStructure)
            {
                isAlly = targetTile.worldEffectOwner == snapshot.ActingPlayerKey;
            }
            else if (actionType == ActionType.AttackUnit)
            {
                isAlly = targetUnit != null && targetUnit.owner == snapshot.ActingPlayerKey;
            }

            if (attacker.attack <= 0 && !isAlly)
            {
                if (!(actionType == ActionType.AttackStructure && attacker.canColonizeEnemyWorldEffects))
                {
                    return -1000f;
                }
            }

            if (actionType == ActionType.AttackFort)
            {
                return ScoreFortAttack(snapshot, attacker);
            }
            if (actionType == ActionType.AttackStructure)
            {
                return ScoreStructureAttack(snapshot, attacker, targetTile);
            }

            if (actionType != ActionType.AttackUnit || targetUnit == null)
            {
                return 0f;
            }

            if (isAlly)
            {
                int missingHp = targetUnit.RuntimeCard != null ? GetCardMissingHp(targetUnit.RuntimeCard) : 0;
                if (missingHp > 0)
                {
                    return 80f + (missingHp * 20f); // High priority to heal damaged units
                }
                return 0f; // Don't heal full HP units
            }

            int targetHp = Mathf.Max(1, targetUnit.health);
            int targetAttack = Mathf.Max(0, targetUnit.attack);
            bool killsTarget = attacker.attack >= targetHp;
            bool survivesCounterAttack = attacker.health > targetAttack;

            float score = 60f;
            score += targetAttack * 25f;
            score += Mathf.Clamp01(attacker.attack / (float)targetHp) * 90f;
            score += ScoreThreatNearOwnFort(snapshot, targetTile);

            if (killsTarget)
            {
                score += 180f;
            }

            if (targetAttack >= attacker.health)
            {
                score += 60f;
            }

            if (survivesCounterAttack)
            {
                score += 45f;
            }
            else if (!killsTarget)
            {
                score -= 90f;
            }

            return score;
        }

        private static float ScoreUfoCowPlacement(ComputerGameSnapshot snapshot, AxialCoord coord)
        {
            if (snapshot?.HexGrid == null)
            {
                return 0f;
            }

            HexTile tile = snapshot.HexGrid.GetTile(coord);
            if (tile == null)
            {
                return 0f;
            }

            List<HexTile> neighbors = HexUtils.GetNeighbors(tile, snapshot.HexGrid);
            for (int i = 0; i < neighbors.Count; i++)
            {
                HexTile neighbor = neighbors[i];
                if (neighbor != null
                    && neighbor.HasWorldEffect()
                    && neighbor.isFieldTile
                    && neighbor.worldEffectOwner == snapshot.OpponentPlayerKey)
                {
                    return 260f + Mathf.Max(0, neighbor.fieldBonusMoneyPerTurn) * 50f;
                }
            }

            return 0f;
        }

        private static float ScoreWorldEffectAttack(ComputerGameSnapshot snapshot, Unit attacker, HexTile targetTile)
        {
            if (snapshot?.HexGrid == null || attacker == null || targetTile == null || !targetTile.HasWorldEffect())
            {
                return 0f;
            }

            float score = 70f + ScoreThreatNearOwnFort(snapshot, targetTile) * 0.5f;

            if (targetTile.isFieldTile)
            {
                int fieldHp = Mathf.Max(1, targetTile.fieldHp);
                int damage = GetEffectiveWorldEffectDamage(attacker, targetTile);
                score += 110f + Mathf.Max(0, targetTile.fieldBonusMoneyPerTurn) * 70f;

                if (damage >= fieldHp)
                {
                    score += 180f;
                }

                return score;
            }

            CardRuntimeState targetCard = snapshot.BoardReader != null ? snapshot.BoardReader.GetCardAt(targetTile.coord) : null;
            int targetHp = targetCard != null ? Mathf.Max(1, GetCardCurrentHp(targetCard)) : 1;
            int targetDamage = targetCard != null ? Mathf.Max(0, GetCardCurrentDamage(targetCard)) : 0;

            score += targetDamage * 20f;
            if (attacker.attack >= targetHp)
            {
                score += 160f;
            }

            return score;
        }

        private static int GetEffectiveWorldEffectDamage(Unit attacker, HexTile targetTile)
        {
            if (attacker == null)
            {
                return 0;
            }

            if (targetTile != null && targetTile.isFieldTile && attacker.sourceCharacterCardData is UfoCowCardData ufoCowCardData)
            {
                return Mathf.Max(1, ufoCowCardData.fieldConsumeAmount);
            }

            return Mathf.Max(0, attacker.attack);
        }

        private static float ScoreFortAttack(ComputerGameSnapshot snapshot, Unit attacker)
        {
            if (snapshot?.OpponentPlayer == null || attacker == null)
            {
                return 0f;
            }

            int opponentFortHp = Mathf.Max(1, snapshot.OpponentPlayer.fortHp);
            int attackDamage = Mathf.Max(0, attacker.attack);
            int remainingFortHp = opponentFortHp - attackDamage;

            if (remainingFortHp <= 0)
            {
                return 10000f;
            }

            float score = 100f;
            score += attackDamage * 30f;

            if (opponentFortHp <= 8)
            {
                score += 120f;
            }

            if (remainingFortHp <= attackDamage)
            {
                score += 180f;
            }

            return score;
        }

        private static float ScoreStructureAttack(ComputerGameSnapshot snapshot, Unit attacker, HexTile targetTile)
        {
            if (snapshot?.BoardReader == null || attacker == null || targetTile == null)
            {
                return 0f;
            }

            CardRuntimeState runtimeTarget = snapshot.BoardReader.GetCardAt(targetTile.coord);
            if (runtimeTarget?.SourceCard is not WorldEffectCardData worldEffectCard)
            {
                return 0f;
            }

            int structureHp = worldEffectCard.structureHp.HasValue ? worldEffectCard.structureHp.Value : 0;
            if (structureHp <= 0)
            {
                return 0f;
            }

            if (attacker.canColonizeEnemyWorldEffects)
            {
                return 400f;
            }

            bool isAlly = targetTile.worldEffectOwner == snapshot.ActingPlayerKey;
            if (isAlly)
            {
                int currentHp = GetCardCurrentHp(runtimeTarget);
                int missingHp = Mathf.Max(0, structureHp - currentHp);
                if (missingHp > 0)
                {
                    return 80f + (missingHp * 20f);
                }
                return 0f;
            }

            int attackDamage = Mathf.Max(0, attacker.attack);
            float score = attackDamage * 25f;

            if (attackDamage >= structureHp)
            {
                score += 150f;
            }

            return score;
        }

        private static float ScoreMoveToAttackThreat(
            ComputerGameSnapshot snapshot,
            Unit unit,
            HexTile destinationTile,
            int movementDistance,
            UnitManager unitManager)
        {
            if (snapshot?.HexGrid == null || unit == null || destinationTile == null || unitManager == null)
            {
                return 0f;
            }

            if (!unit.isReadyToAttack || unit.hasAttackedThisTurn)
            {
                return 0f;
            }

            int remainingMovementAfterMove = unit.GetRemainingMovement() - movementDistance;
            if (remainingMovementAfterMove <= 0)
            {
                return 0f;
            }

            int attackRange = Mathf.Max(0, unit.attackRange);
            if (attackRange <= 0)
            {
                return 0f;
            }

            List<HexTile> futureTargets = HexUtils.GetTilesInRange(destinationTile, attackRange, snapshot.HexGrid);
            float bestThreatScore = 0f;

            for (int i = 0; i < futureTargets.Count; i++)
            {
                HexTile futureTarget = futureTargets[i];
                if (!CanThreatenTargetFromMove(snapshot, unit, futureTarget, unitManager, out Unit targetUnit, out ActionType actionType))
                {
                    continue;
                }

                float threatScore = ScoreAttackTarget(snapshot, unit, futureTarget, targetUnit, actionType);
                if (actionType == ActionType.AttackFort)
                {
                    threatScore = threatScore >= 10000f ? 9000f : 140f + threatScore * 0.45f;
                }
                else if (actionType == ActionType.AttackWorldEffect)
                {
                    threatScore = 140f + threatScore * 0.5f;
                }
                else
                {
                    threatScore = 160f + threatScore * 0.55f;
                }

                bestThreatScore = Mathf.Max(bestThreatScore, threatScore);
            }

            return bestThreatScore;
        }

        private static bool CanThreatenTargetFromMove(
            ComputerGameSnapshot snapshot,
            Unit attacker,
            HexTile targetTile,
            UnitManager unitManager,
            out Unit targetUnit,
            out ActionType actionType)
        {
            targetUnit = null;
            actionType = ActionType.EndTurn;

            if (snapshot == null || attacker == null || targetTile == null || unitManager == null)
            {
                return false;
            }

            if (GetAttackType(attacker.sourceCharacterCardData) == AttackType.HealFix)
            {
                return false;
            }

            if (targetTile.tileType == "fort" && targetTile.owner == snapshot.OpponentPlayerKey)
            {
                actionType = ActionType.AttackFort;
                return CanProfileTarget(GetAttackTarget(attacker.sourceCharacterCardData), targetIsAir: false);
            }

            if (targetTile.HasWorldEffect()
                && !targetTile.isMineTile
                && targetTile.worldEffectOwner == snapshot.OpponentPlayerKey
                && CanProfileTarget(GetAttackTarget(attacker.sourceCharacterCardData), targetIsAir: false))
            {
                actionType = ActionType.AttackWorldEffect;
                return true;
            }

            if (targetTile.tileType != "unit" || targetTile.owner != snapshot.OpponentPlayerKey)
            {
                return false;
            }

            targetUnit = unitManager.FindUnitOnTile(targetTile);
            if (targetUnit == null)
            {
                return false;
            }

            bool targetIsAir = IsAirUnit(targetUnit);
            if (!CanProfileTarget(GetAttackTarget(attacker.sourceCharacterCardData), targetIsAir))
            {
                return false;
            }

            actionType = ActionType.AttackUnit;
            return true;
        }

        private static float ScoreThreatNearOwnFort(ComputerGameSnapshot snapshot, HexTile targetTile)
        {
            if (snapshot?.HexGrid == null || targetTile == null)
            {
                return 0f;
            }

            if (!TryGetFortTile(snapshot, snapshot.ActingPlayerKey, out AxialCoord fortCoord))
            {
                return 0f;
            }

            HexTile fortTile = snapshot.HexGrid.GetTile(fortCoord);
            int distanceToFort = HexUtils.GetHexDistance(targetTile, fortTile);
            if (distanceToFort < 0)
            {
                return 0f;
            }

            if (distanceToFort <= 2)
            {
                return 250f;
            }

            if (distanceToFort <= 4)
            {
                return 120f;
            }

            return Mathf.Max(0f, 6 - distanceToFort) * 18f;
        }

        private static AttackType GetAttackType(CharacterCardData cardData)
        {
            return cardData != null ? cardData.attackType : AttackType.Melee;
        }

        private static global::AttackTarget GetAttackTarget(CharacterCardData cardData)
        {
            return cardData != null ? cardData.attackTarget : global::AttackTarget.Ground;
        }

        private static bool CanProfileTarget(global::AttackTarget attackTarget, bool targetIsAir)
        {
            if (attackTarget == global::AttackTarget.Both)
            {
                return true;
            }

            if (targetIsAir)
            {
                return attackTarget == global::AttackTarget.Air;
            }

            return attackTarget == global::AttackTarget.Ground;
        }

        private static bool IsAirUnit(Unit unit)
        {
            return unit != null
                && unit.sourceCharacterCardData != null
                && unit.sourceCharacterCardData.movementType == MovementType.Flying;
        }

        private static float ScoreLaneSpread(ComputerGameSnapshot snapshot, AxialCoord coord)
        {
            // (abdo :) Penalize rows already occupied by allied units so new spawns do not stack on one lane.
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.ActingPlayerKey))
            {
                return 0f;
            }

            Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            if (units == null || units.Length == 0)
            {
                return 0f;
            }

            float score = 0f;
            for (int i = 0; i < units.Length; i++)
            {
                Unit unit = units[i];
                if (unit == null || unit.owner != snapshot.ActingPlayerKey || unit.currentTile == null)
                {
                    continue;
                }

                int rowDistance = Mathf.Abs(unit.currentTile.coord.r - coord.r);
                if (rowDistance == 0)
                {
                    score -= 42f;
                }
                else if (rowDistance == 1)
                {
                    score -= 16f;
                }
                else if (rowDistance == 2)
                {
                    score -= 6f;
                }
            }

            return score;
        }

        private static float GetCenterBandScore(ComputerGameSnapshot snapshot, int row)
        {
            if (snapshot?.HexGrid == null || snapshot.HexGrid.gridHeight <= 1)
            {
                return 0f;
            }

            float centerRow = (snapshot.HexGrid.gridHeight - 1) * 0.5f;
            float centerBandRadius = snapshot.HexGrid.gridHeight >= 9 ? 2f : 1f;
            float distanceFromCenter = Mathf.Abs(row - centerRow);

            if (distanceFromCenter <= centerBandRadius)
            {
                return 1f;
            }

            float falloffDistance = Mathf.Max(1f, centerRow - centerBandRadius);
            return Mathf.Clamp01(1f - (distanceFromCenter - centerBandRadius) / falloffDistance);
        }

        private static float GetStableTileJitter(AxialCoord coord)
        {
            int hash = coord.q * 73856093 ^ coord.r * 19349663;
            hash = Mathf.Abs(hash % 1000);
            return hash / 1000f;
        }

        private static float GetCenterRowScore(ComputerGameSnapshot snapshot, int row)
        {
            if (snapshot?.HexGrid == null || snapshot.HexGrid.gridHeight <= 1)
            {
                return 0f;
            }

            float centerRow = (snapshot.HexGrid.gridHeight - 1) * 0.5f;
            float maxDistance = Mathf.Max(1f, centerRow);
            float distanceFromCenter = Mathf.Abs(row - centerRow);

            return Mathf.Clamp01(1f - distanceFromCenter / maxDistance);
        }

        private static int GetForwardProgress(ComputerGameSnapshot snapshot, HexTile startTile, HexTile destinationTile)
        {
            if (snapshot?.HexGrid == null || startTile == null || destinationTile == null)
            {
                return 0;
            }

            int startColumn = snapshot.HexGrid.AxialToOffsetColumn(startTile.coord);
            int destinationColumn = snapshot.HexGrid.AxialToOffsetColumn(destinationTile.coord);

            return snapshot.ActingPlayerKey == "enemy"
                ? startColumn - destinationColumn
                : destinationColumn - startColumn;
        }


        // Ali: reusable validator checks the real Fort tile, so AI fort targets need the Fort coordinate.
        private static bool TryGetOpponentFortTile(ComputerGameSnapshot snapshot, out AxialCoord fortTile)
        {
            return TryGetFortTile(snapshot, snapshot?.OpponentPlayerKey, out fortTile);
        }

        private static bool TryGetFortTile(ComputerGameSnapshot snapshot, string ownerKey, out AxialCoord fortTile)
        {
            fortTile = default;

            if (snapshot?.HexGrid == null || string.IsNullOrWhiteSpace(ownerKey))
            {
                return false;
            }

            for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
            {
                for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    HexTile tile = snapshot.HexGrid.GetTile(coord);

                    if (tile != null && tile.tileType == "fort" && tile.owner == ownerKey)
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

            CardTarget fortTarget = new CardTarget { type = CardTargetType.EnemyFort };
            return GetSpellDamageAmount(runtimeCard, fortTarget) >= snapshot.OpponentPlayer.fortHp;
        }

        private static bool ShouldDefend(ComputerGameSnapshot snapshot, int col)
        {
            if (snapshot?.ActingPlayer == null || snapshot.HexGrid == null)
            {
                return false;
            }

            // Defend if the action is in the AI's half of the board
            int midPoint = snapshot.HexGrid.gridWidth / 2;
            return snapshot.ActingPlayerKey == "enemy" ? col >= midPoint : col < midPoint;
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

        private static bool ShouldDevelopBoard(ComputerGameSnapshot snapshot, int col)
        {
            if (snapshot?.ActingPlayer == null)
            {
                return false;
            }

            return snapshot.ActingPlayer.fortHp >= 8 && IsDefensiveColumn(snapshot, col);
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
