using System.Collections.Generic;
using UnityEngine;

public sealed class SpellManager : MonoBehaviour
{
    private static readonly Freeze freeze = new Freeze();
    private static readonly LightningStrike lightningStrike = new LightningStrike();
    private static readonly Sabotage sabotage = new Sabotage();
    private static readonly TaxCollection taxCollection = new TaxCollection();
    private static readonly TwoXSpeed twoXSpeed = new TwoXSpeed();
    private readonly Dictionary<CardRuntimeState, Spell> spellsBySource = new Dictionary<CardRuntimeState, Spell>();
    private readonly List<Spell> activePersistentSpells = new List<Spell>();
    private FortGame.UI.HUDManager hudManager;

    public static SpellManager GetOrCreate()
    {
        SpellManager manager = FindFirstObjectByType<SpellManager>();
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("SpellManager");
        return managerObject.AddComponent<SpellManager>();
    }

    public Spell FindSpell(CardRuntimeState sourceCard)
    {
        if (sourceCard == null)
        {
            return null;
        }

        spellsBySource.TryGetValue(sourceCard, out Spell spell);
        return spell;
    }

    public bool Remove(CardRuntimeState sourceCard)
    {
        if (sourceCard == null)
        {
            return false;
        }

        if (!spellsBySource.TryGetValue(sourceCard, out Spell spell))
        {
            return false;
        }

        spellsBySource.Remove(sourceCard);
        activePersistentSpells.Remove(spell);
        if (spell != null)
        {
            spell.Remove();
        }

        return true;
    }

    public void ConsumePersistentDurations(string ownerKey = null)
    {
        for (int i = activePersistentSpells.Count - 1; i >= 0; i--)
        {
            Spell spell = activePersistentSpells[i];
            if (spell == null)
            {
                activePersistentSpells.RemoveAt(i);
                continue;
            }

            string durationOwnerKey = ResolveDurationOwnerKey(spell);
            if (!string.IsNullOrWhiteSpace(ownerKey) && durationOwnerKey != ownerKey)
            {
                continue;
            }

            bool expired = spell.ConsumeDurationTurn();
            if (!expired)
            {
                continue;
            }

            activePersistentSpells.RemoveAt(i);
            if (spell.sourceCard != null)
            {
                spellsBySource.Remove(spell.sourceCard);
            }

            NotifySpellExpired(spell);
            spell.Remove();
        }
    }

    public CardEffectResult ApplyDamageSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (sabotage.IsMatch(sourceCard))
        {
            if (!string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                spell.durationOwnerKey = target.targetPlayerId;
            }

            return CompleteSpell(spell, sabotage.Apply(sourceCard, target));
        }

        if (taxCollection.IsMatch(sourceCard))
        {
            if (!string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                spell.durationOwnerKey = target.targetPlayerId;
            }

            return CompleteSpell(spell, taxCollection.Apply(sourceCard, target));
        }

        if (lightningStrike.IsMatch(sourceCard))
        {
            return CompleteSpell(spell, lightningStrike.Apply(context, sourceCard, target, safeAmount));
        }

        if (target.type == CardTargetType.EnemyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CompleteSpell(spell, CardEffectResult.Failure("NO_TARGET_PLAYER", "Damage spell needs a fort owner id."));
            }

            context.Writer.ApplyFortDamage(target.targetPlayerId, safeAmount);
            return CompleteSpell(spell, CardEffectResult.Success("Fort damage applied.", damageDealt: safeAmount));
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Damage spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        context.Writer.ApplyDamage(target.targetCard, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Damage applied.", damageDealt: safeAmount));
    }

    public CardEffectResult ApplyHealSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (target.type == CardTargetType.AllyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CompleteSpell(spell, CardEffectResult.Failure("NO_TARGET_PLAYER", "Heal spell needs a fort owner id."));
            }

            context.Writer.ApplyFortHeal(target.targetPlayerId, safeAmount);
            return CompleteSpell(spell, CardEffectResult.Success("Fort heal applied.", healApplied: safeAmount));
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Heal spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        context.Writer.ApplyHeal(target.targetCard, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Heal applied.", healApplied: safeAmount));
    }

    public CardEffectResult ApplyBuffSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int healAmount, int damageBoostAmount, int speedBoostAmount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Buff spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        if (twoXSpeed.IsMatch(sourceCard))
        {
            return CompleteSpell(spell, twoXSpeed.Apply(sourceCard, target));
        }

        int safeHeal = Mathf.Max(0, healAmount);
        int safeDamageBoost = Mathf.Max(0, damageBoostAmount);
        int safeSpeedBoost = Mathf.Max(0, speedBoostAmount);
        bool didSomething = false;

        if (safeHeal > 0)
        {
            context.Writer.ApplyHeal(target.targetCard, safeHeal);
            didSomething = true;
        }

        if (safeDamageBoost > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, safeDamageBoost);
            didSomething = true;
        }

        if (safeSpeedBoost > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, safeSpeedBoost);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_BUFF_VALUES", "Set at least one buff value above zero."));
        }

        return CompleteSpell(spell, CardEffectResult.Success("Buff applied.", healApplied: safeHeal));
    }

    public CardEffectResult ApplyDebuffSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int damageAmount, int damageReductionAmount, int speedReductionAmount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Debuff spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        int safeDamage = Mathf.Max(0, damageAmount);
        int safeDamageReduction = Mathf.Max(0, damageReductionAmount);
        int safeSpeedReduction = Mathf.Max(0, speedReductionAmount);

        if (freeze.IsMatch(sourceCard))
        {
            if (!string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                spell.durationOwnerKey = target.targetPlayerId;
            }

            return CompleteSpell(spell, freeze.Apply(sourceCard, target));
        }

        bool didSomething = false;

        if (safeDamage > 0)
        {
            context.Writer.ApplyDamage(target.targetCard, safeDamage);
            didSomething = true;
        }

        if (safeDamageReduction > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, -safeDamageReduction);
            didSomething = true;
        }

        if (safeSpeedReduction > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, -safeSpeedReduction);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_DEBUFF_VALUES", "Set at least one debuff value above zero."));
        }

        return CompleteSpell(spell, CardEffectResult.Success("Debuff applied.", damageDealt: safeDamage));
    }

    public CardEffectResult ApplyUtilitySpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int movementDelta)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Utility spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        int safeDelta = Mathf.Max(0, movementDelta);
        if (safeDelta <= 0)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_UTILITY_VALUE", "Set movement delta above zero."));
        }

        context.Writer.ModifyMovement(target.targetCard, safeDelta);
        return CompleteSpell(spell, CardEffectResult.Success("Utility effect applied."));
    }

    public CardEffectResult ApplyBoostSpell(CardEffectContext context, CardRuntimeState sourceCard, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, default, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing."));
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.AddRevenue(context.ActingPlayerKey, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Income boost applied.", revenueGained: safeAmount));
    }

    public CardEffectResult ApplySummonSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, bool requireTileToBeEmpty)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetType(target, CardTargetType.Tile, "Summon spell needs a tile target.", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        if (context.Board != null)
        {
            if (!CardEffectGuards.TryRequireBoardAndValidTile(context, target.tile, "Target tile is invalid.", out failure))
            {
                return CompleteSpell(spell, failure);
            }

            if (requireTileToBeEmpty && !CardEffectGuards.TryRequireTileEmpty(context, target.tile, "Target tile is occupied.", out failure))
            {
                return CompleteSpell(spell, failure);
            }
        }

        context.Writer.ManifestCard(sourceCard, target.tile);
        return CompleteSpell(spell, CardEffectResult.Success("Summon applied."));
    }

    private bool TryPrepareSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, out Spell spell, out CardEffectResult failure)
    {
        spell = null;

        if (!CardEffectGuards.TryRequireContextAndWriter(context, out failure))
        {
            return false;
        }

        if (!CardEffectGuards.TryRequireSourceCard(sourceCard, out failure))
        {
            return false;
        }

        if (!(sourceCard.SourceCard is SpellCardData))
        {
            failure = CardEffectResult.Failure("NO_SPELL_CARD", "Spell manager can only resolve spell cards.");
            return false;
        }

        spell = CreateOrUpdateSpell(sourceCard, ResolveSpellOwner(context, target), target);
        failure = default;
        return true;
    }

    private Spell CreateOrUpdateSpell(CardRuntimeState sourceCard, string owner, CardTarget target)
    {
        if (spellsBySource.TryGetValue(sourceCard, out Spell existing) && existing != null)
        {
            existing.Initialize(owner, sourceCard, target);
            return existing;
        }

        GameObject spellObject = new GameObject($"Spell_{sourceCard.SourceCard.DisplayName}");
        spellObject.transform.SetParent(transform, true);

        Spell spell = spellObject.AddComponent<Spell>();
        spell.Initialize(owner, sourceCard, target);

        spellsBySource[sourceCard] = spell;
        return spell;
    }

    private CardEffectResult CompleteSpell(Spell spell, CardEffectResult result)
    {
        if (spell == null)
        {
            return result;
        }

        if (!result.Succeeded)
        {
            if (spell.sourceCard != null)
            {
                Remove(spell.sourceCard);
            }
            else
            {
                spell.Remove();
            }

            return result;
        }

        spell.MarkResolved();
        NotifySpellResolved(spell, result);

        if (spell.remainingDurationTurns > 0)
        {
            if (!activePersistentSpells.Contains(spell))
            {
                activePersistentSpells.Add(spell);
            }

            return result;
        }

        if (spell.sourceCard != null)
        {
            Remove(spell.sourceCard);
        }
        else
        {
            spell.Remove();
        }

        return result;
    }

    private static string ResolveSpellOwner(CardEffectContext context, CardTarget target)
    {
        if (context != null && !string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return context.ActingPlayerKey;
        }

        if (!string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return target.targetPlayerId;
        }

        return "none";
    }

    public bool TryGetFieldIncomeRecipient(string fieldOwnerKey, HexTile incomeTile, out string recipientOwnerKey)
    {
        recipientOwnerKey = fieldOwnerKey;

        if (string.IsNullOrWhiteSpace(fieldOwnerKey) || incomeTile == null || !incomeTile.HasWorldEffect() || !incomeTile.isFieldTile)
        {
            return false;
        }

        for (int i = activePersistentSpells.Count - 1; i >= 0; i--)
        {
            Spell spell = activePersistentSpells[i];
            if (spell == null
                || !taxCollection.IsMatch(spell.sourceCard)
                || ResolveDurationOwnerKey(spell) != fieldOwnerKey
                || !taxCollection.MatchesIncomeTile(spell, incomeTile))
            {
                continue;
            }

            recipientOwnerKey = spell.owner;
            return !string.IsNullOrWhiteSpace(recipientOwnerKey) && recipientOwnerKey != fieldOwnerKey;
        }

        return false;
    }

    public bool IsWorldEffectDisabled(WorldEffect worldEffect)
    {
        if (worldEffect == null || worldEffect.currentTile == null || string.IsNullOrWhiteSpace(worldEffect.owner))
        {
            return false;
        }

        for (int i = activePersistentSpells.Count - 1; i >= 0; i--)
        {
            Spell spell = activePersistentSpells[i];
            if (spell == null
                || !sabotage.IsMatch(spell.sourceCard)
                || ResolveDurationOwnerKey(spell) != worldEffect.owner
                || !sabotage.MatchesWorldEffect(spell, worldEffect))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && ReferenceEquals(unit.RuntimeCard, card))
            {
                return unit;
            }
        }

        return null;
    }

    private void NotifySpellResolved(Spell spell, CardEffectResult result)
    {
        if (spell == null || spell.sourceCard == null || spell.sourceCard.SourceCard == null)
        {
            return;
        }

        string targetName = GetTargetDisplayName(spell.target);
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        string message = $"{spell.sourceCard.SourceCard.DisplayName} played on {targetName}";

        if (result.DamageDealt > 0 || result.HealApplied > 0)
        {
            int? currentHp = null;
            if (spell.target.targetCard != null)
            {
                if (spell.target.targetCard.CurrentHp.HasValue)
                {
                    currentHp = spell.target.targetCard.CurrentHp.Value;
                }
                
                // If the target is dead, CurrentHp might be null, but we can check if it was destroyed
                if (!currentHp.HasValue && result.DamageDealt > 0)
                {
                    currentHp = 0;
                }
            }
            else if (spell.target.type == CardTargetType.AllyFort || spell.target.type == CardTargetType.EnemyFort)
            {
                var gm = FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    if (gm.player1 != null && (spell.target.targetPlayerId == "player" || spell.target.targetPlayerId == gm.player1.playerName))
                    {
                        currentHp = gm.player1.fortHp;
                    }
                    else if (gm.player2 != null && (spell.target.targetPlayerId == "enemy" || spell.target.targetPlayerId == gm.player2.playerName))
                    {
                        currentHp = gm.player2.fortHp;
                    }
                }
            }

            if (result.DamageDealt > 0)
            {
                message += $" (-{result.DamageDealt} HP)";
            }
            else if (result.HealApplied > 0)
            {
                message += $" (+{result.HealApplied} HP)";
            }

            if (currentHp.HasValue)
            {
                message += $" [HP: {currentHp.Value}]";
            }
        }

        BroadcastSpellAnnouncement(message);
    }

    private void NotifySpellExpired(Spell spell)
    {
        if (spell == null || spell.sourceCard == null || spell.sourceCard.SourceCard == null)
        {
            return;
        }

        string cardName = spell.sourceCard.SourceCard.DisplayName;
        string ownerLabel = GetOwnerLabel(spell.owner);
        BroadcastNotification($"{cardName} effect from {ownerLabel} has ended.");
    }

    private void BroadcastNotification(string message, string logPrefix = "SpellManager")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<FortGame.UI.HUDManager>();
        }

        if (hudManager != null)
        {
            hudManager.ShowInfo(message);
        }

        Debug.Log($"[{logPrefix}] {message}");
    }

    private void BroadcastSpellAnnouncement(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<FortGame.UI.HUDManager>();
        }

        if (hudManager != null)
        {
            hudManager.ShowSpellAnnouncement(message);
        }

        Debug.Log($"[SpellAnnouncement] {message}");
    }

    private static string GetOwnerLabel(string owner)
    {
        if (owner == PlayerKeyResolver.PlayerOneKey)
        {
            return "Player";
        }

        if (owner == PlayerKeyResolver.PlayerTwoKey)
        {
            return "Enemy";
        }

        return string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner;
    }

    private static string ResolveDurationOwnerKey(Spell spell)
    {
        if (spell == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(spell.durationOwnerKey) ? spell.owner : spell.durationOwnerKey;
    }

    private static string GetTargetDisplayName(CardTarget target)
    {
        if (target.targetCard != null && target.targetCard.SourceCard != null)
        {
            return target.targetCard.SourceCard.DisplayName;
        }

        switch (target.type)
        {
            case CardTargetType.AllyFort:
                return "Fort";

            case CardTargetType.EnemyFort:
                return "Fort";

            case CardTargetType.Tile:
                return $"tile ({target.tile.q},{target.tile.r})";

            default:
                return string.Empty;
        }
    }
}
