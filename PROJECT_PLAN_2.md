# Project Plan 2 - Remaining Work

Date: 2026-05-21

This plan only lists work that still remains after the current checks.

Verified baseline:

- Unity 6000.3.10f1 opens the project in batchmode and script compilation succeeds.
- No Unity compile errors were found in `Temp/codex-unity-batch.log`.
- Ali confirmed the final v1 match flow works: `Income -> Buy/Discard -> Play -> EndTurn`.
- Buying cards stays random for v1; no shop/card-choice UI is required.
- Game Over panel and Restart button work in `SampleScene`.

## Ali - Game Logic, Rules, and Balance

- Finalize v1 balance values.
  - Tune `MainGameConfig.asset`: starting money, Fort HP, income, buy cost, discard reward, hand limit.
  - Tune card costs and stats with Fatine so random buying is playable and not full of free/empty cards.

- Lock the final v1 rules in documentation.
  - Flow is now final: `Income -> Buy/Discard -> Play -> EndTurn`.
  - Attack is part of Play.
  - Buy remains random.
  - Update any old docs that still mention a separate normal Attack phase or undecided buy behavior.

## Abdo - Hex Board and Combat

- Finish board mechanics for unfinished World Effects.
  - `Wall`: block movement and/or attacks according to the final rule.
  - `Watch tower`: attack or damage enemy units in range.
  - `Hospital`: heal nearby allied units.
  - `Fog`: apply the movement/visibility penalty rule.
  - `Anti-air tower`: counter flying units.

- Finish flying and anti-air combat rules.
  - `Dragon` needs board/combat behavior for flying.
  - Melee units such as Knight/Spearman should not hit flying units if that remains the final rule.
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
  - The AI should not blindly spend all useful-looking actions just because ending turn scores very low.
  - Add a practical stopping rule so the AI can end turn after good actions are exhausted.

- Improve AI priorities.
  - Prefer lethal Fort damage.
  - Defend its Fort when low.
  - Avoid wasting strong cards on weak targets.
  - Prefer income/board setup early when useful.

- Finish player-facing UI polish.
  - Clear invalid action feedback.
  - Clear selected-card and selected-target feedback.
  - Make turn owner, money, Fort HP, hand count, and phase readable in the final scene.

## Fatine - Card System, Card Data, and Effects

- Complete unfinished Character card data.
  - `Spearman`, `Priest`, `Dragon`, and `Engineer` still have `maxHp: 0` and/or `attackDamage: 0`.
  - Several Character cards still have `cost: 0`; keep only the cards that are intentionally free.

- Complete unfinished Spell cards.
  - `Freeze`: needs real effect mapping, power/duration, and movement-lock behavior.
  - `Lightning strike`: needs real damage value and effect mapping.
  - `Revival`: needs revive behavior or removal from v1 scope.
  - `Sabotage`: needs building-disable behavior or removal from v1 scope.
  - `Tax collection`: needs field/resource-steal behavior or removal from v1 scope.

- Complete unfinished World Effect card data and effect mapping.
  - `Wall`, `Watch tower`, `Hospital`, `Fog`, and `Anti-air tower` currently exist as assets but still need real gameplay values/effects.
  - Any World Effect with no real v1 behavior should be removed from the active random pool until implemented.

- Clean the random card library for v1.
  - `MainCardLibrary.asset` should only include cards that are playable with real stats/effects.
  - Random buy should not give placeholder cards with zero stats or missing effects.

- Align card effect IDs with actual effects.
  - Cards that need `effect.damage`, `effect.heal`, `effect.buff`, `effect.debuff`, `effect.utility`, `effect.income_boost`, or `effect.summon` must have the correct `effectId`.
  - Cards with empty `effectId` should only stay empty if they intentionally have no runtime effect.

## Final Demo Checklist

- Main playable scene is `Assets/Scenes/SampleScene.unity`.
- Project compiles in Unity without script errors.
- Player can complete full turns with the final flow.
- Computer can complete turns without breaking the match.
- Random buying only gives usable v1 cards.
- Game ends when a Fort reaches 0 HP.
- Game Over screen appears with the winner.
- Restart starts a clean new match.
- No placeholder zero-stat cards appear in the final random pool.
