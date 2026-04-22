using UnityEngine;

public enum DebugEffectMode
{
    MoveSourceToDiscard,
    ManifestSourceOnTile,
    DamageTargetCard,
    HealTargetCard,
    AddRevenueToActor
}

public sealed class DebugCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "debug.effect.basic";
    [SerializeField] private DebugEffectMode mode = DebugEffectMode.MoveSourceToDiscard;
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (context == null)
        {
            return CardEffectResult.Failure("NO_CONTEXT", "Effect context is missing.");
        }

        if (context.Writer == null)
        {
            return CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
        }

        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            return CardEffectResult.Failure("NO_CARD", "Source card is missing.");
        }

        int safeAmount = Mathf.Max(0, amount);

        if (mode == DebugEffectMode.MoveSourceToDiscard)
        {
            context.Writer.MoveCardToZone(sourceCard, CardZone.Discard);
            return CardEffectResult.Success("Moved source card to discard.");
        }

        if (mode == DebugEffectMode.ManifestSourceOnTile)
        {
            if (target.type != CardTargetType.Tile)
            {
                return CardEffectResult.Failure("WRONG_TARGET", "Manifest mode requires a tile target.");
            }

            if (context.Board != null && !context.Board.IsTileValid(target.tile))
            {
                return CardEffectResult.Failure("INVALID_TILE", "Target tile is not valid.");
            }

            context.Writer.ManifestCard(sourceCard, target.tile);
            return CardEffectResult.Success($"Manifested source card at {target.tile}.");
        }

        if (mode == DebugEffectMode.DamageTargetCard)
        {
            if (target.targetCard == null)
            {
                return CardEffectResult.Failure("NO_TARGET_CARD", "Damage mode requires target.targetCard.");
            }

            context.Writer.ApplyDamage(target.targetCard, safeAmount);
            return CardEffectResult.Success("Applied debug damage.", damageDealt: safeAmount);
        }

        if (mode == DebugEffectMode.HealTargetCard)
        {
            if (target.targetCard == null)
            {
                return CardEffectResult.Failure("NO_TARGET_CARD", "Heal mode requires target.targetCard.");
            }

            context.Writer.ApplyHeal(target.targetCard, safeAmount);
            return CardEffectResult.Success("Applied debug heal.", healApplied: safeAmount);
        }

        if (mode == DebugEffectMode.AddRevenueToActor)
        {
            context.Writer.AddRevenue(context.ActingPlayerKey, safeAmount);
            return CardEffectResult.Success("Added debug revenue to acting player.", revenueGained: safeAmount);
        }

        return CardEffectResult.Failure("UNKNOWN_MODE", "Unsupported debug effect mode.");
    }
}
