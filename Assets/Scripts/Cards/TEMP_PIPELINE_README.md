# Temporary Card Play Pipeline Notes

This is a temporary implementation guide for Fatine's production card-play pipeline.

## Implemented Files
- `Assets/Scripts/Cards/Contracts/CardPlayRequest.cs`
- `Assets/Scripts/Cards/Contracts/CardPlayResult.cs`
- `Assets/Scripts/Cards/Contracts/ICardPlayPipeline.cs`
- `Assets/Scripts/Cards/Pipeline/CardPlayPipeline.cs`

## Pipeline Order
The pipeline enforces this order in one place:
1. Validate target (`ICardTargetValidator.Validate`)
2. Spend cost (`ICardStateWriter.TrySpendCost`)
3. Apply effect (`ICardEffect.Apply`)
4. Finalize source-card zone

## Finalize Rule
- If the source card is manifested on board after effect execution, final zone is `Board`.
- Otherwise, source card is moved to `Discard`.

This keeps spells in discard and keeps successfully manifested cards on board.

## Failure Behavior
- Validation failure: no spending, no effect execution.
- Spend failure: no effect execution.
- Effect failure after spending: returns `EFFECT_FAILED_AFTER_SPEND` and leaves card in current zone.

## Integration Example
```csharp
var pipeline = new CardPlayPipeline();

var request = new CardPlayRequest
{
    ActingPlayerId = actingPlayerId,
    OpponentPlayerId = opponentPlayerId,
    SourceCard = runtimeCard,
    Target = target,
    Board = boardReader,
    Writer = stateWriter,
    Validator = targetValidator,
    Effect = cardEffect
};

CardPlayResult result = pipeline.Play(request);
if (!result.Succeeded)
{
    // Show result.ReasonCode / result.Message in UI
}
```

## Temporary Scope
- This pipeline lives only in Fatine-owned area.
- It does not directly edit Ali, Abdo, or Rabie systems.
- Next integration step is wiring callers to this pipeline through one adapter point.
