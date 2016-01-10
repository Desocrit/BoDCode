using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EffectCondition;

public class Attribute
{
    public enum AttributeName
    {
        Attack,
        Defense,
        Agility,
        Intelligence,
        Constitution,
        Luck,
        Lifesteal,
        Replenishment,
        Healing,
        Retribution,
        Movement,
        Bleed,
        Weaken,
        Slaying,
        Massacre
    }

    public static AttributeName[] primaryAttributes = 
    {
        AttributeName.Attack, AttributeName.Defense,
        AttributeName.Constitution, AttributeName.Agility,
        AttributeName.Intelligence, AttributeName.Luck
    };

    private AttributeName _name;
    public AttributeName Name { get { return _name; } }

    private int _value;
    public int Value
    {
        get { return _value; }
        set
        {
            _value = value;
            foreach(KeyValuePair<StatModifier, float> modifier in statModifiers)
            {
                modifier.Key.modifier = modifier.Value * Value;
                target.UpdateStats();
                if(appliedEffect != null)
                {
                    target.RemoveEffect(appliedEffect, intercept);
                    UpdateEffect();
                    target.AddEffect(appliedEffect, intercept);
                }
            }
        }
    }

    private Character target;

    private Dictionary<StatModifier, float> statModifiers;

    private Effect appliedEffect;
    private Character.Intercept intercept;

    public Attribute(AttributeName name, Character target, int initialValue)
    {
        _name = name;
        _value = initialValue;
        this.target = target;

        appliedEffect = null;
        statModifiers = new Dictionary<StatModifier, float>();

        InitialiseModifier();
        foreach(KeyValuePair<StatModifier, float> statMod in statModifiers)
            target.AddModifier(statMod.Key);

        if(appliedEffect != null)
            target.AddEffect(appliedEffect, intercept);
    }

    public void InitialiseModifier()
    {
        switch(Name)
        {
            case AttributeName.Attack:
                AddStatModifier(Statistic.AttackDamage, 0.01f, true);
                if(target.Stats.secondaryType == Resource.Name.Energy)
                    AddStatModifier(Statistic.MaxSecondary, 0.01f, true);
                return;
            case AttributeName.Defense:
                AddStatModifier(Statistic.MagicDefense, 0.01f, true);
                AddStatModifier(Statistic.MeleeDefense, 0.01f, true);
                return;
            case AttributeName.Agility:
                AddStatModifier(Statistic.AttackSpeed, 0.01f, true);
                return;
            case AttributeName.Intelligence:
                AddStatModifier(Statistic.ExperienceValue, 0.01f, true);
                if(target.Stats.secondaryType == Resource.Name.Mana)
                    AddStatModifier(Statistic.MaxSecondary, 0.01f, true);
                return;
            case AttributeName.Constitution:
                AddStatModifier(Statistic.MaxHP, 0.01f, true);
                if(target.Stats.secondaryType == Resource.Name.Focus)
                    AddStatModifier(Statistic.MaxSecondary, 0.01f, true);
                return;
            case AttributeName.Luck:
                AddStatModifier(Statistic.Luck, 0.01f, true);
                if(target.Stats.secondaryType == Resource.Name.Focus)
                    AddStatModifier(Statistic.CritChance, 0.01f, true);
                return;
            case AttributeName.Movement:
                AddStatModifier(Statistic.MovementSpeed, 0.01f, true);
                return;
            case AttributeName.Lifesteal:
            case AttributeName.Replenishment:
            case AttributeName.Healing:
            case AttributeName.Retribution:
            case AttributeName.Bleed:
            case AttributeName.Weaken:
            case AttributeName.Slaying:
            case AttributeName.Massacre:
                UpdateEffect();
                return;
            default:
                return;
        }
    }

    public void AddStatModifier(Statistic stat, float mod, bool isMult)
    {
        statModifiers.Add(new StatModifier(stat, mod * Value, isMult), mod);
    }

    public void UpdateEffect()
    {
        switch(Name)
        {
            case AttributeName.Lifesteal:
                appliedEffect = new Effect(EffectType.DamageOrHeal,
                                           -Value * 0.01f, true);
                intercept = Character.Intercept.DealDamage;
                return;
            case AttributeName.Replenishment:
                appliedEffect = new Effect(EffectType.ModifySecondary,
                                           -Value * 0.01f, true);
                intercept = Character.Intercept.DealDamage;
                return;
            case AttributeName.Healing:
                appliedEffect = new Effect(EffectType.Multiply,
                                           1 + Value * 0.01f, true);
                intercept = Character.Intercept.DealDamage;
                return;
            case AttributeName.Retribution:
                appliedEffect = new Effect(EffectType.DamageOrHeal,
                                           Value * 0.01f, false);
                intercept = Character.Intercept.ReceiveDamage;
                return;
            case AttributeName.Bleed:
            case AttributeName.Weaken:
            case AttributeName.Slaying:
                var boost = new Effect(EffectType.Multiply,
                                       1 + Value * 0.02f, false);
                appliedEffect = new ConditionalEffect(boost,
                                                      new CharacterCondition());
                intercept = Character.Intercept.ReceiveDamage;
                return;
            case AttributeName.Massacre:
            default:
                return;
        }
    }

    public bool IsPrimary(AttributeName att)
    {
        return Array.IndexOf(primaryAttributes, att) != -1;
    }
}