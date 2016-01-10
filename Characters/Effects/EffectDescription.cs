using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using EffectCondition;

[Serializable]
// For editor use.
public class EffectDescription
{
    // Description of the effect in question.
    public EffectType effectType;
    public bool affectsCaster;
    public float magnitude;

    // If duration =/= 0, TimedEffect is produced.
    public float duration;
    public int repeats;

    public ConditionDescription[] conditions;
    public StatModifier[] statModifiers;

    private Effect generatedEffect;

    // TODO: Statusses

    private EffectDescription()
    {
        generatedEffect = null;
    }

    public Effect GetEffect()
    {
        if(generatedEffect != null)
            return generatedEffect;
        if(statModifiers.Length != 0 && duration > 0 && 
            effectType == EffectType.ApplyStatModifier)
        {
            return GenerateBuff();
        }
        Effect result;
        if(effectType == EffectType.ApplyStatModifier ||
            effectType == EffectType.ApplyStatModifier)
        {
            List<Effect> effects = new List<Effect>();
            foreach(StatModifier mod in statModifiers)
                effects.Add(new Effect(effectType, affectsCaster, mod));
            result = new Effect(effects);
        } else
            result = new Effect(effectType, magnitude, affectsCaster);
        if(duration > 0)
            result = new TimedEffect(duration, repeats, null, result);
        generatedEffect = ApplyConditions(result);
        return generatedEffect;
    }

    private Effect GenerateBuff()
    {
        List<Effect> startEffects = new List<Effect>();
        List<Effect> endEffects = new List<Effect>();
        EffectType remove = EffectType.RemoveStatModifier;
        foreach(StatModifier mod in statModifiers)
        {
            startEffects.Add(new Effect(effectType, affectsCaster, mod));
            endEffects.Add(new Effect(remove, affectsCaster, mod));
        }
        return ApplyConditions(new TimedEffect(duration, repeats, 
                                               new Effect(startEffects),
                                               new Effect(endEffects)));
    }

    private Effect ApplyConditions(Effect effect)
    {
        if(conditions.Length == 0)
            return effect;
        if(conditions.Length == 1)
            return new ConditionalEffect(effect, conditions[0]);
        Condition combined = new CombinedCondition(Combination.AllTrue,
                                                   conditions);
        return new ConditionalEffect(effect, combined);
    }
}