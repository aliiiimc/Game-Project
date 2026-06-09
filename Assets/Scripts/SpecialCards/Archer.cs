public class Archer : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is ArcherCardData;
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        if (unit == null)
        {
            return 0;
        }

        return unitCardData != null
            ? UnityEngine.Mathf.Max(0, unitCardData.attackRange)
            : (unit != null ? UnityEngine.Mathf.Max(0, unit.attackRange) : 0);
    }

    public static bool ShouldPlayProjectile(CharacterCardData unitCardData)
    {
        return unitCardData is ArcherCardData archerCardData
            && archerCardData.projectileVisuals != null
            && archerCardData.projectileVisuals.projectilePrefab != null;
    }
}
