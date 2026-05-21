using System.Collections.Generic;
using UnityEngine;

public class WorldEffectManager : MonoBehaviour
{
    private readonly Dictionary<HexTile, WorldEffect> worldEffectsByTile = new Dictionary<HexTile, WorldEffect>();

    public bool TryPlaceFromCard(HexTile tile, string owner, CardRuntimeState card, out WorldEffect worldEffect)
    {
        worldEffect = null;

        if (tile == null || card == null || !(card.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        if (!tile.IsEmpty())
        {
            return false;
        }

        worldEffect = CreateWorldEffectFromCard(tile, owner, card, worldEffectCard);
        return worldEffect != null;
    }

    public bool TryColonize(HexTile tile, string newOwner)
    {
        if (tile == null || string.IsNullOrWhiteSpace(newOwner))
        {
            return false;
        }

        if (tile.tileType != "worldEffect" || tile.owner == "none" || tile.owner == newOwner)
        {
            return false;
        }

        tile.PlaceWorldEffect(newOwner);

        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect existing) && existing != null)
        {
            existing.owner = newOwner;
            return true;
        }

        return true;
    }

    public bool TryReplace(HexTile tile, string owner, CardRuntimeState card, out WorldEffect worldEffect)
    {
        worldEffect = null;

        if (tile == null || card == null || !(card.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        if (tile.tileType != "worldEffect")
        {
            return false;
        }

        worldEffect = CreateWorldEffectFromCard(tile, owner, card, worldEffectCard);
        return worldEffect != null;
    }

    public bool Remove(HexTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect worldEffect))
        {
            worldEffectsByTile.Remove(tile);
            if (worldEffect != null)
            {
                worldEffect.RemoveFromBoard();
            }
            else
            {
                tile.RemoveUnit();
            }
            return true;
        }

        if (tile.tileType == "worldEffect")
        {
            tile.RemoveUnit();
            return true;
        }

        return false;
    }

    public bool TrySetFieldData(HexTile tile, string clusterId, int hpPerTile, int bonusMoneyPerTurn = 1)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetFieldData(clusterId, hpPerTile, bonusMoneyPerTurn);
        return true;
    }

    public bool TrySetMineData(HexTile tile, int damage)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetMineData(damage);
        return true;
    }

    public bool TryClearSpecialData(HexTile tile)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.ClearWorldEffectSpecialData();
        return true;
    }

    public bool TrySetCampData(HexTile tile)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetCampData();
        return true;
    }

    public bool TryDamageField(HexTile tile, int amount)
    {
        if (tile == null || tile.tileType != "worldEffect" || !tile.isFieldTile)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        tile.fieldHp -= safeAmount;

        if (tile.fieldHp <= 0)
        {
            return Remove(tile);
        }

        return true;
    }

    // Backward-compatible wrapper while call sites migrate.
    public WorldEffect SpawnWorldEffectFromCard(HexTile tile, string owner, CardRuntimeState card)
    {
        return TryPlaceFromCard(tile, owner, card, out WorldEffect worldEffect) ? worldEffect : null;
    }

    // Backward-compatible wrapper while call sites migrate.
    public bool RemoveWorldEffect(HexTile tile)
    {
        return Remove(tile);
    }

    public WorldEffect FindWorldEffectOnTile(HexTile tile)
    {
        if (tile == null)
        {
            return null;
        }

        worldEffectsByTile.TryGetValue(tile, out WorldEffect worldEffect);
        return worldEffect;
    }

    public bool TryGetAttackProfile(HexTile sourceTile, out AttackType attackType, out AttackTarget attackTarget)
    {
        attackType = AttackType.Projectile;
        attackTarget = AttackTarget.Ground;

        if (sourceTile == null || sourceTile.tileType != "worldEffect")
        {
            return false;
        }

        WorldEffect worldEffect = FindWorldEffectOnTile(sourceTile);
        if (worldEffect == null || worldEffect.sourceCard == null || !(worldEffect.sourceCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        attackType = worldEffectCard.attackType;
        attackTarget = worldEffectCard.attackTarget;
        return true;
    }

    public bool CanTargetWithProfile(HexTile sourceTile, bool targetIsAir)
    {
        if (!TryGetAttackProfile(sourceTile, out _, out AttackTarget attackTarget))
        {
            return false;
        }

        if (attackTarget == AttackTarget.Both)
        {
            return true;
        }

        if (targetIsAir)
        {
            return attackTarget == AttackTarget.Air;
        }

        return attackTarget == AttackTarget.Ground;
    }

    private WorldEffect CreateWorldEffectFromCard(HexTile tile, string owner, CardRuntimeState card, WorldEffectCardData worldEffectCard)
    {
        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect existing) && existing != null)
        {
            existing.InitializeFromCard(card);
            existing.PlaceOnTile(tile, owner, worldEffectCard.manifestedSprite);
            return existing;
        }

        GameObject worldEffectObject = new GameObject($"WorldEffect_{worldEffectCard.DisplayName}");
        worldEffectObject.transform.SetParent(transform, true);

        WorldEffect worldEffect = worldEffectObject.AddComponent<WorldEffect>();
        worldEffect.InitializeFromCard(card);
        worldEffect.PlaceOnTile(tile, owner, worldEffectCard.manifestedSprite);

        worldEffectsByTile[tile] = worldEffect;
        return worldEffect;
    }

    private static bool IsOwnedWorldEffectTile(HexTile tile)
    {
        return tile != null
            && tile.tileType == "worldEffect"
            && !string.IsNullOrWhiteSpace(tile.owner)
            && tile.owner != "none";
    }
}
