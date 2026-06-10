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

        if (!tile.CanPlaceWorldEffect())
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

        if (!tile.HasWorldEffect() || tile.worldEffectOwner == "none" || tile.worldEffectOwner == newOwner)
        {
            return false;
        }

        string clusterId = tile.isFieldTile ? tile.fieldClusterId : string.Empty;
        bool colonizedAtLeastOne = false;

        HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        Color? sideColor = null;
        if (grid != null)
        {
            sideColor = newOwner == "player" ? grid.playerSideColor : grid.enemySideColor;
        }

        HexTile[] allTiles = UnityEngine.Object.FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile t = allTiles[i];
            bool shouldColonize = false;

            if (t == tile)
            {
                shouldColonize = true;
            }
            else if (!string.IsNullOrEmpty(clusterId) && t.isFieldTile && t.fieldClusterId == clusterId)
            {
                shouldColonize = true;
            }

            if (shouldColonize && t.HasWorldEffect() && t.worldEffectOwner != newOwner)
            {
                t.SetWorldEffectOwner(newOwner);
                if (worldEffectsByTile.TryGetValue(t, out WorldEffect existing) && existing != null)
                {
                    existing.owner = newOwner;
                }

                if (sideColor.HasValue)
                {
                    t.SetBaseColor(sideColor.Value);
                }
                colonizedAtLeastOne = true;
            }
        }

        return colonizedAtLeastOne;
    }

    public bool TryReplace(HexTile tile, string owner, CardRuntimeState card, out WorldEffect worldEffect)
    {
        worldEffect = null;

        if (tile == null || card == null || !(card.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        if (!tile.HasWorldEffect() || tile.HasUnitOccupant())
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

        if (tile.HasWorldEffect() && tile.isFieldTile && !string.IsNullOrWhiteSpace(tile.fieldClusterId))
        {
            return RemoveFieldCluster(tile.fieldClusterId);
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
                tile.RemoveWorldEffect();
            }
            return true;
        }

        if (tile.HasWorldEffect())
        {
            tile.RemoveWorldEffect();
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
        if (tile == null || !tile.HasWorldEffect() || !tile.isFieldTile)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        List<HexTile> clusterTiles = GetFieldClusterTiles(tile);
        if (clusterTiles.Count == 0)
        {
            return false;
        }

        int currentClusterHp = GetFieldClusterHp(tile);
        int remainingClusterHp = Mathf.Max(0, currentClusterHp - safeAmount);

        if (remainingClusterHp <= 0)
        {
            return RemoveFieldCluster(tile.fieldClusterId);
        }

        SyncFieldClusterHp(clusterTiles, remainingClusterHp);
        return true;
    }

    public bool TryDamageWorldEffect(HexTile tile, int amount, out int dealtDamage)
    {
        // (abdo :) Shared damage path for fields and structures, used by normal combat and special attacks.
        dealtDamage = 0;

        if (tile == null || !tile.HasWorldEffect() || amount <= 0)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        if (tile.isFieldTile)
        {
            int fieldHpBefore = Mathf.Max(0, tile.fieldHp);
            bool damaged = TryDamageField(tile, safeAmount);
            if (!damaged)
            {
                return false;
            }

            int fieldHpAfter = tile.HasWorldEffect() && tile.isFieldTile
                ? Mathf.Max(0, tile.fieldHp)
                : 0;
            dealtDamage = Mathf.Max(0, fieldHpBefore - fieldHpAfter);
            if (dealtDamage > 0)
            {
                var hud = UnityEngine.Object.FindFirstObjectByType<FortGame.UI.HUDManager>();
                hud?.ShowSpellAnnouncement($"A field took {dealtDamage} damage. [HP: {fieldHpAfter}]");
            }
            return true;
        }

        WorldEffect worldEffect = FindWorldEffectOnTile(tile);
        if (worldEffect == null)
        {
            dealtDamage = safeAmount;
            return Remove(tile);
        }

        int hpBefore = worldEffect.sourceCard != null && worldEffect.sourceCard.CurrentHp.HasValue
            ? Mathf.Max(0, worldEffect.sourceCard.CurrentHp.Value)
            : Mathf.Max(0, worldEffect.health);

        if (worldEffect.sourceCard != null)
        {
            worldEffect.sourceCard.ApplyDamage(safeAmount);
            worldEffect.health = worldEffect.sourceCard.CurrentHp.HasValue
                ? worldEffect.sourceCard.CurrentHp.Value
                : Mathf.Max(0, worldEffect.health - safeAmount);
        }
        else
        {
            worldEffect.health = Mathf.Max(0, worldEffect.health - safeAmount);
        }

        dealtDamage = Mathf.Max(0, hpBefore - Mathf.Max(0, worldEffect.health));
        if (dealtDamage > 0)
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<FortGame.UI.HUDManager>();
            string effectName = worldEffect.sourceCard != null ? worldEffect.sourceCard.SourceCard.DisplayName : "World effect";
            hud?.ShowSpellAnnouncement($"{effectName} took {dealtDamage} damage. [HP: {worldEffect.health}]");
        }

        if (worldEffect.health <= 0)
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

        if (sourceTile == null || !sourceTile.HasWorldEffect())
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
            existing.PlaceOnTile(
                tile,
                owner,
                worldEffectCard.manifestedSprite,
                worldEffectCard.allowsUnitPassThrough,
                worldEffectCard.allowsUnitOccupancy,
                worldEffectCard.worldEffectOpacity);
            return existing;
        }

        GameObject worldEffectObject = new GameObject($"WorldEffect_{worldEffectCard.DisplayName}");
        worldEffectObject.transform.SetParent(transform, true);

        WorldEffect worldEffect = worldEffectObject.AddComponent<WorldEffect>();
        worldEffect.InitializeFromCard(card);
        worldEffect.PlaceOnTile(
            tile,
            owner,
            worldEffectCard.manifestedSprite,
            worldEffectCard.allowsUnitPassThrough,
            worldEffectCard.allowsUnitOccupancy,
            worldEffectCard.worldEffectOpacity);

        worldEffectsByTile[tile] = worldEffect;
        return worldEffect;
    }

    private static bool IsOwnedWorldEffectTile(HexTile tile)
    {
        return tile != null
            && tile.HasWorldEffect()
            && !string.IsNullOrWhiteSpace(tile.worldEffectOwner)
            && tile.worldEffectOwner != "none";
    }

    private List<HexTile> GetFieldClusterTiles(HexTile tile)
    {
        List<HexTile> clusterTiles = new List<HexTile>();
        if (tile == null || !tile.HasWorldEffect() || !tile.isFieldTile)
        {
            return clusterTiles;
        }

        string clusterId = tile.fieldClusterId;
        if (string.IsNullOrWhiteSpace(clusterId))
        {
            clusterTiles.Add(tile);
            return clusterTiles;
        }

        HexTile[] allTiles = UnityEngine.Object.FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile candidate = allTiles[i];
            if (candidate != null
                && candidate.HasWorldEffect()
                && candidate.isFieldTile
                && candidate.fieldClusterId == clusterId)
            {
                clusterTiles.Add(candidate);
            }
        }

        return clusterTiles;
    }

    private int GetFieldClusterHp(HexTile tile)
    {
        List<HexTile> clusterTiles = GetFieldClusterTiles(tile);
        int highestHp = 0;
        for (int i = 0; i < clusterTiles.Count; i++)
        {
            highestHp = Mathf.Max(highestHp, Mathf.Max(0, clusterTiles[i].fieldHp));
        }

        return highestHp;
    }

    private void SyncFieldClusterHp(List<HexTile> clusterTiles, int remainingClusterHp)
    {
        int safeRemainingHp = Mathf.Max(0, remainingClusterHp);
        for (int i = 0; i < clusterTiles.Count; i++)
        {
            if (clusterTiles[i] != null)
            {
                clusterTiles[i].fieldHp = safeRemainingHp;
            }
        }
    }

    private bool RemoveFieldCluster(string clusterId)
    {
        if (string.IsNullOrWhiteSpace(clusterId))
        {
            return false;
        }

        List<HexTile> clusterTiles = new List<HexTile>();
        HexTile[] allTiles = UnityEngine.Object.FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            if (tile != null
                && tile.HasWorldEffect()
                && tile.isFieldTile
                && tile.fieldClusterId == clusterId)
            {
                clusterTiles.Add(tile);
            }
        }

        bool removedAny = false;
        for (int i = 0; i < clusterTiles.Count; i++)
        {
            HexTile tile = clusterTiles[i];
            if (tile == null)
            {
                continue;
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
                    tile.RemoveWorldEffect();
                }
            }
            else
            {
                tile.RemoveWorldEffect();
            }

            removedAny = true;
        }

        return removedAny;
    }
}
