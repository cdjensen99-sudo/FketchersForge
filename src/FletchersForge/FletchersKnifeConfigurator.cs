using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class FletchersKnifeConfigurator
{
    internal static void Configure(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_itemType = ItemDrop.ItemData.ItemType.OneHandedWeapon;
        shared.m_toolTier = 0;
        shared.m_weight = ModConstants.KnifeWeight;
        shared.m_teleportable = true;
        shared.m_maxStackSize = 1;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_useDurability = false;
        shared.m_maxDurability = 0;
        shared.m_durabilityDrain = 0f;
        shared.m_blockPower = 0;
        shared.m_blockPowerPerLevel = 0;
        shared.m_blockable = false;
        shared.m_deflectionForce = 0;
        shared.m_deflectionForcePerLevel = 0;
        shared.m_timedBlockBonus = 1f;
        shared.m_blockAdrenaline = 0;
        shared.m_perfectBlockAdrenaline = 0;
        shared.m_perfectBlockStaminaRegen = 0;
        shared.m_perfectBlockStatusEffect = null;
        shared.m_maxAdrenaline = 0;
        shared.m_fullAdrenalineSE = null;
        shared.m_attackForce = 0;
        shared.m_backstabBonus = 1f;
        shared.m_armor = 0;
        shared.m_armorPerLevel = 0;
        shared.m_attackStatusEffect = null;
        shared.m_attackStatusEffectChance = 0f;
        shared.m_equipStatusEffect = null;
        shared.m_secondaryAttack = null;

        shared.m_damages = new HitData.DamageTypes();
        shared.m_damagesPerLevel = new HitData.DamageTypes();

        ItemDrop templatePrefab = PrefabManager.Instance.GetPrefab("KnifeCopper")?.GetComponent<ItemDrop>();
        if (templatePrefab?.m_itemData?.m_shared?.m_attack == null)
        {
            shared.m_attack = null;
            FletchersForgePlugin.Log?.LogWarning("Fletcher's knife: KnifeCopper attack template missing; knife has no swing.");
            return;
        }

        shared.m_attack = CloneAttack(templatePrefab.m_itemData.m_shared.m_attack);
        shared.m_attack.m_damageMultiplier = 0f;
        shared.m_attack.m_forceMultiplier = 0f;
        shared.m_attack.m_raiseSkillAmount = 0f;
        shared.m_attack.m_attackStamina = 0;
        shared.m_attack.m_selfDamage = 0;
    }

    private static Attack CloneAttack(Attack source)
    {
        Attack clone = new Attack();
        foreach (var field in typeof(Attack).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            field.SetValue(clone, field.GetValue(source));
        }

        return clone;
    }
}
