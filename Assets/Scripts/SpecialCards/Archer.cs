public class Archer : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Archer");
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        if (unit == null)
        {
            return 0;
        }

        int bonusAttackRange = 2;
        if (unitCardData is ArcherCardData archerCardData)
        {
            bonusAttackRange = UnityEngine.Mathf.Max(0, archerCardData.bonusAttackRange);
        }

        int baseAttackRange = unitCardData != null
            ? UnityEngine.Mathf.Max(0, unitCardData.attackRange)
            : (unit != null ? UnityEngine.Mathf.Max(0, unit.attackRange) : 0);

        return baseAttackRange + bonusAttackRange;
    }
}
