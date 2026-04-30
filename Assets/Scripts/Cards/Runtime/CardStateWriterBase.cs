using UnityEngine;

public abstract class CardStateWriterBase : ICardStateWriter
{
    public abstract bool TrySpendCost(string playerId, int amount);
    public abstract void AddRevenue(string playerId, int amount);

    public virtual void MoveCardToZone(CardRuntimeState card, CardZone zone)
    {
        if (card == null)
        {
            return;
        }

        card.MoveToZone(zone);
        LogTransaction($"MoveCardToZone: {card.SourceCard.DisplayName} -> {zone}.");
    }

    public virtual void ManifestCard(CardRuntimeState card, AxialCoord tile)
    {
        if (card == null)
        {
            return;
        }

        card.ManifestOnBoard(tile);
        LogTransaction($"ManifestCard: {card.SourceCard.DisplayName} at {tile}.");
    }

    public virtual void ApplyDamage(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        card.ApplyDamage(amount);
        LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} amount={Mathf.Max(0, amount)}.");
    }

    public virtual void ApplyHeal(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        card.ApplyHeal(amount);
        LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} amount={Mathf.Max(0, amount)}.");
    }

    public virtual void ApplyFortDamage(string playerId, int amount)
    {
        LogTransaction($"ApplyFortDamage ignored by {GetType().Name}: player='{playerId}' amount={Mathf.Max(0, amount)}.");
    }

    public virtual void ApplyFortHeal(string playerId, int amount)
    {
        LogTransaction($"ApplyFortHeal ignored by {GetType().Name}: player='{playerId}' amount={Mathf.Max(0, amount)}.");
    }

    public virtual void ModifyDamage(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        card.ModifyDamage(delta);
        LogTransaction($"ModifyDamage: {card.SourceCard.DisplayName} delta={delta}.");
    }

    public virtual void ModifyMovement(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        card.ModifyMovement(delta);
        LogTransaction($"ModifyMovement: {card.SourceCard.DisplayName} delta={delta}.");
    }

    protected virtual void LogTransaction(string message)
    {
    }
}
