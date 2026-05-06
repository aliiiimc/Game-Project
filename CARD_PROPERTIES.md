# Card Properties Table

Last updated: 2026-05-06

## Character Cards

| Asset | Card Name | Cost | Max HP | Attack | Starts Ready | Move Capacity | Validator Id | Effect Id |
|---|---|---:|---:|---:|---|---:|---|---|
| `Character/Archer.asset` | Archer | 3 | 50 | 15 | Yes | 2 | (default from `CardData`) | (default from `CardData`) |
| `Character/European King.asset` | European King | 5 | 20 | 0 | No | 4 | (default from `CardData`) | (default from `CardData`) |
| `Character/Knight.asset` | Knight | 3 | 70 | 20 | Yes | 2 | (default from `CardData`) | (default from `CardData`) |
| `Character/Miner.asset` | Miner | 20 | 30 | 10 | No | 10 | (default from `CardData`) | (default from `CardData`) |
| `Character/UFO Cow.asset` | UFO Cow | 0 | 50 | 20 | No | 3 | (default from `CardData`) | (default from `CardData`) |

## Spell Cards

| Asset | Card Name | Cost | Effect Type | Effect Power | Effect Duration | Move Capacity | Validator Id | Effect Id |
|---|---|---:|---|---:|---:|---:|---|---|
| `Spell/2 speed.asset` | 2x speed | 4 | Buff | 1 | 3 | None | `target.rules.reusable` | `effect.buff` |
| `Spell/Scout Utility.asset` | Scout Utility | 2 | Utility | 1 | 0 | None | `target.rules.reusable` | `effect.utility` |

## World Effect Cards

| Asset | Card Name | Cost | Category | Structure HP | Structure Damage | Revenue/Turn | Duration | Move Capacity | Validator Id | Effect Id |
|---|---|---:|---|---|---|---|---:|---|---|---|
| `World Effect/Corn field.asset` | (empty) | 0 | Structure | None | None | None | 0 | None | `target.rules.reusable` | `effect.summon` |
| `World Effect/Mines.asset` | (empty) | 0 | Structure | None | None | None | 0 | None | `target.rules.reusable` | `effect.summon` |

## Notes

- `MainCardLibrary.asset` currently references 5 cards in its `cards` list.
- Character card assets above currently rely on inherited defaults for `validatorId` and `effectId` unless you set explicit values in Inspector.

## Debug Testing Recipe 

- Minimum scene objects:
  - `CardDebugLab` with `CardDebugRunner`
  - `ValidatorsHost` with `ReusableTargetRulesValidator`
  - `EffectsHost` with production effects (`Summon`, `Buff`, `Damage`, `Heal`, `Debuff`, `IncomeBoost`, `Utility`)
- Summon validation/effect test:
  - Source card: character/world card
  - Target type: `Tile`
  - Effect behavior: `SummonCardEffect`
  - If real `HexGrid` gives `INVALID_TILE`, clear `Board Source` to use fake board
- Unit-target spell test:
  - Source card: spell (for example `2 speed`, `Scout Utility`)
  - Target type: `AllyUnit` or `EnemyUnit`
  - Enable `Use Dummy Target Card` in `CardDebugRunner`
  - Set `Dummy Target Card Data` to a character card
  - Keep `Dummy Target On Board = true`
  - Set `Target Player Id` to `player` (ally) or `enemy` (enemy)
- Expected outcomes:
  - Success: `Play PASSED` with zone/effect payload logs
  - Failure: stable reason codes (`NO_TARGET_CARD`, `WRONG_TARGET_PLAYER`, `INVALID_TILE`, etc.)
