// Write API used by effects to mutate game/card state.
public interface ICardStateWriter
{
    bool TrySpendCost(string playerId, int amount);
    void MoveCardToZone(CardRuntimeState card, CardZone zone);
    void ManifestCard(CardRuntimeState card, AxialCoord tile);
    void ApplyDamage(CardRuntimeState card, int amount);
    void ApplyHeal(CardRuntimeState card, int amount);
    void AddRevenue(string playerId, int amount);
}
