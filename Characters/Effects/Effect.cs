using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Effect
{
    // Caster, target, type, in, out.
    private Func<Character, Character, DamageType, float, float> effect;

    protected Effect()
    {
    }

    public Effect(EffectType type, float modifier)
    {
        effect = generateEffect(type, modifier, false, null, null);
    }

    public Effect(EffectType type, float modifier, bool affectsCaster)
    {
        effect = generateEffect(type, modifier, affectsCaster, null, null);
    }

    public Effect(EffectType type, bool affectsCaster, StatModifier statMod)
    {
        effect = generateEffect(type, 0, affectsCaster, statMod, null);
    }

    public Effect(IEnumerable<Effect> causedEffects)
    {
        effect = generateEffect(EffectType.CallEffect, 0,
                                false, null, causedEffects);
    }

    public Effect(Func<Character,Character, DamageType, float, float> effect)
    {
        this.effect = effect;
    }

    public virtual float Call(Character caster, Character other,
                      DamageType damageType, float value)
    {
        if(caster == null)
            return value;
        return effect(caster, other, damageType, value);
    }

    public static Func<Character, Character, DamageType, float, float>
        generateEffect(EffectType type, float modifier, bool affectsCaster, 
                       StatModifier statMod, IEnumerable<Effect> causedEffects)
    {
        switch(type)
        {
            case EffectType.Increase:
                return (c1, c2, dt, v) => v + modifier;
            case EffectType.Multiply:
                return (c1, c2, dt, v) => v * modifier;
            case EffectType.DamageOrHeal:
                if(affectsCaster)
                    return (c1, c2, dt, v) => 
                    {
                        c1.DamageOrHeal(c1, modifier, dt, false);
                        return v;
                    };
                else
                    return (c1, c2, dt, v) => 
                    {
                        c1.DamageOrHeal(c2, modifier, dt, false);
                        return v;
                    };
            case EffectType.DamageOrHealUnmodified:
                if(affectsCaster)
                    return (c1, c2, dt, v) => 
                    {
                        c1.AttackTarget(c1, modifier, dt);
                        return v;
                    };
                else
                    return (c1, c2, dt, v) => 
                    {
                        c1.AttackTarget(c2, modifier, dt);
                        return v;
                    };
            case EffectType.ModifySecondary:
                if(affectsCaster)
                    return (c1, c2, dt, v) => 
                    {
                        c1.ModifySecondary(c1, modifier, dt, false);
                        return v;
                    };
                else
                    return (c1, c2, dt, v) => 
                    {
                        c2.ModifySecondary(c2, modifier, dt, false);
                        return v;
                    };
            case EffectType.ApplyStatModifier:
                if(affectsCaster)
                    return (c1, c2, dt, v) => 
                    { 
                        c1.AddModifier(statMod);
                        return v;
                    };
                else
                    return (c1, c2, dt, v) => 
                    { 
                        c2.AddModifier(statMod);
                        return v;
                    };
            case EffectType.RemoveStatModifier:
                if(affectsCaster)
                    return (c1, c2, dt, v) => 
                    { 
                        c1.RemoveModifier(statMod);
                        return v;
                    };
                else
                    return (c1, c2, dt, v) => 
                    { 
                        c2.RemoveModifier(statMod);
                        return v;
                    };
            case EffectType.CallEffect:
                return (c1, c2, dt, v) => 
                {
                    foreach(Effect causedEffect in causedEffects)
                        causedEffect.Call(c1, c2, dt, v);
                    return v;
                };
            default:
                return (c1, c2, dt, v) => v;
        }
    }

}