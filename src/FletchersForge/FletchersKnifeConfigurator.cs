using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

namespace FletchersForge;

internal static class FletchersKnifeConfigurator
{
    internal static void Configure(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_itemType = ItemDrop.ItemData.ItemType.Tool;
        shared.m_toolTier = 0;
        shared.m_weight = ModConstants.KnifeWeight;
        shared.m_teleportable = true;
        shared.m_maxStackSize = 1;
        shared.m_maxQuality = 1;
        shared.m_value = 0;
        shared.m_useDurability = true;
        shared.m_maxDurability = 1;
        shared.m_durabilityDrain = 1f;
        shared.m_blockPower = 0;
        shared.m_blockPowerPerLevel = 0;
        shared.m_armor = 0;
        shared.m_armorPerLevel = 0;
        shared.m_attackStatusEffect = null;
        shared.m_attackStatusEffectChance = 0f;
        shared.m_equipStatusEffect = null;
        shared.m_secondaryAttack = null;

        shared.m_damages = new HitData.DamageTypes();
        shared.m_damages.m_pierce = 1f;
        shared.m_damagesPerLevel = new HitData.DamageTypes();

        ItemDrop templatePrefab = PrefabManager.Instance.GetPrefab("KnifeCopper")?.GetComponent<ItemDrop>();
        if (templatePrefab?.m_itemData?.m_shared?.m_attack == null)
        {
            FletchersForgePlugin.Log?.LogWarning("Fletcher's knife: KnifeCopper attack template missing.");
            return;
        }

        shared.m_attack = CloneAttack(templatePrefab.m_itemData.m_shared.m_attack);
        shared.m_attack.m_damageMultiplier = 1f;
        shared.m_attack.m_raiseSkillAmount = 0f;
        shared.m_attack.m_attackStamina = 0;
        shared.m_attack.m_selfDamage = 0;
    }

    private static Attack CloneAttack(Attack source)
    {
        Attack clone = new Attack();
        foreach (FieldInfo field in typeof(Attack).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            field.SetValue(clone, field.GetValue(source));
        }

        return clone;
    }
}
