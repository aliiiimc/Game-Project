# Project Plan 2 - Remaining Work

Date: 2026-05-21

This plan only lists work that still remains after the current checks.

Verified baseline:

- Unity 6000.3.10f1 opens the project in batchmode and script compilation succeeds.
- No Unity compile errors were found in `Temp/codex-unity-batch.log`.
- Ali confirmed the final v1 match flow works: `Income -> Buy/Discard -> Play -> EndTurn`.
- Game Over panel and Restart button work in `SampleScene`.

New buy/economy directive:

- Buying should stay random, but not from the full card library.
- During Buy phase, the player chooses a cost tier/amount to spend, for example `2`, `3`, `4`, or `5`.
- The game then gives one random card whose `CardData.cost` exactly matches the chosen amount.
- The player pays the chosen amount, not one global `buyCost`.
- The one-buy-per-turn rule stays.
- If the player does not have enough money, the buy is refused.
- If no active card exists for that chosen cost, the buy is refused.
- This keeps randomness while making economy cards such as Wheat Field useful, because more money unlocks higher-cost random pools.
- The computer player must use the same rule by choosing an affordable non-empty cost tier.

## Ali - Game Logic, Rules, and Balance

- Finalize v1 balance values.
  - Tune `MainGameConfig.asset`: starting money, Fort HP, income, discard reward, hand limit, and any remaining global economy values.
  - Replace the old single global buy-cost balance with cost-tier buying.
  - Tune card costs and stats with Fatine so each buy tier has useful cards and no placeholder/free cards unless intentionally free.

- Lock the final v1 rules in documentation.
  - Flow is now final: `Income -> Buy/Discard -> Play -> EndTurn`.
  - Attack is part of Play.
  - Buy remains random, but the random pool is filtered by the cost tier chosen by the player.
  - Update any old docs that still mention a separate normal Attack phase or undecided buy behavior.

## Abdo - Hex Board and Combat

- Board effects now active in v1.
  - `Wall`, `Watch tower`, `Hospital`, and `Anti-air tower` are now implemented and in the active card pool.
  - `Fog` is intentionally excluded from v1.

- Finish flying and anti-air combat rules.
  - `Dragon` now has flying behavior; keep the combat edge cases under test.
  - Verify whether melee units such as Knight/Spearman should continue to miss flying units in the final rule set.
  - Ranged/projectile or anti-air sources should be able to hit flying units.

- Finish structure combat behavior.
  - World Effects with HP should be damageable and removable consistently.
  - Structure ownership changes and destruction should update the board tile state without leaving stale objects.

- Clean main-scene board setup for the final demo.
  - Main gameplay should use `SampleScene`.
  - Debug-only board behavior should stay out of the final playable scene.

## Rabie - Computer Opponent and UI

- Complete the AI turn behavior.
  - The computer currently plays during Play phase; it still needs final v1 behavior for buy/discard if the enemy is expected to use economy like the player.
  - AI buy logic should choose an affordable non-empty cost tier, then receive a random card from that tier.
  - The AI should not blindly spend all useful-looking actions just because ending turn scores very low.
  - Add a practical stopping rule so the AI can end turn after good actions are exhausted.

- Improve AI priorities.
  - Prefer lethal Fort damage.
  - Defend its Fort when low.
  - Avoid wasting strong cards on weak targets.
  - Prefer income/board setup early when useful.

- Finish player-facing UI polish.
  - Add a clear way to choose the buy cost tier, either buy buttons per tier or a cost selector plus Buy button.
  - Clear invalid action feedback.
  - Clear selected-card and selected-target feedback.
  - Make turn owner, money, Fort HP, hand count, and phase readable in the final scene.

## Fatine - Card System, Card Data, and Effects

- Character cards are now in the active pool with real stats.
  - Keep tuning costs and combat values only if playtests show a balance problem.

- Spell cards are now implemented in v1.
  - `Freeze`, `Lightning strike`, `Revival`, `Sabotage`, and `Tax collection` are now active cards with working effect paths.

- World Effect cards are now implemented in v1.
  - `Wall`, `Watch tower`, `Hospital`, and `Anti-air tower` now have real gameplay values/effects.
  - `Fog` is not part of the active random pool.

- Clean the random card library for v1.
  - `MainCardLibrary.asset` now includes only playable cards for the current v1 set.
  - Random buy should not give placeholder cards with zero stats or missing effects.
  - Each active buy tier should contain enough valid cards to make random buying feel fair.

- Align card effect IDs with actual effects.
  - Cards that need `effect.damage`, `effect.heal`, `effect.buff`, `effect.debuff`, `effect.utility`, `effect.income_boost`, or `effect.summon` must have the correct `effectId`.
  - Cards with empty `effectId` should only stay empty if they intentionally have no runtime effect.

## Final Demo Checklist

- Main playable scene is `Assets/Scenes/SampleScene.unity`.
- Project compiles in Unity without script errors.
- Player can complete full turns with the final flow.
- Computer can complete turns without breaking the match.
- Random buying only gives usable v1 cards from the chosen cost tier.
- Fog is not present in the active random pool.
- Game ends when a Fort reaches 0 HP.
- Game Over screen appears with the winner.
- Restart starts a clean new match.
- No placeholder zero-stat cards appear in the final random pool.
