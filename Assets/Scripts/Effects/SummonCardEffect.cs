using UnityEngine;

public sealed class SummonCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.summon";
    [SerializeField] private bool requireTileToBeEmpty = true;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireSourceCard(sourceCard, out failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetType(target, CardTargetType.Tile, "Summon effect needs a tile target.", out failure))
        {
            return failure;
        }

        if (context.Board != null)
        {
            if (!CardEffectGuards.TryRequireBoardAndValidTile(context, target.tile, "Target tile is invalid.", out failure))
            {
                return failure;
            }

            if (requireTileToBeEmpty && !CardEffectGuards.TryRequireTileEmpty(context, target.tile, "Target tile is occupied.", out failure))
            {
                return failure;
            }
        }

        context.Writer.ManifestCard(sourceCard, target.tile);
        return CardEffectResult.Success("Summon applied.");
    }
}
