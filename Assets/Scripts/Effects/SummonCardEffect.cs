using UnityEngine;

public sealed class SummonCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.summon";
    [SerializeField] private bool requireTileToBeEmpty = true;

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

        if (target.type != CardTargetType.Tile)
        {
            return CardEffectResult.Failure("WRONG_TARGET", "Summon effect needs a tile target.");
        }

        if (context.Board != null)
        {
            if (!context.Board.IsTileValid(target.tile))
            {
                return CardEffectResult.Failure("INVALID_TILE", "Target tile is invalid.");
            }

            if (requireTileToBeEmpty && context.Board.IsTileOccupied(target.tile))
            {
                return CardEffectResult.Failure("OCCUPIED_TILE", "Target tile is occupied.");
            }
        }

        context.Writer.ManifestCard(sourceCard, target.tile);
        return CardEffectResult.Success("Summon applied.");
    }
}
