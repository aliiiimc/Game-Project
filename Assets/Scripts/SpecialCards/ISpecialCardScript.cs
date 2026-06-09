public interface ISpecialCardScript
{
    bool IsMatch(Unit unit, CharacterCardData unitCardData);
    int GetAttackRange(Unit unit, CharacterCardData unitCardData);
    bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner);
    bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner);
    bool ConsumeMoveAction(Unit unit, CharacterCardData unitCardData);
    void OnBeforeMove(Unit unit, CharacterCardData unitCardData);
    void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile);
    void OnAfterSpawn(Unit unit, CharacterCardData unitCardData);
    void OnOwnerTurnStart(Unit unit, CharacterCardData unitCardData);
    AttackType GetAttackType(Unit unit, CharacterCardData unitCardData);
}
