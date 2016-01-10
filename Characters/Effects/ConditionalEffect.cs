using UnityEngine;
using System.Collections;
using EffectCondition;

public class ConditionalEffect : Effect
{
    private Condition condition;
    private Effect baseEffect;
    
    public ConditionalEffect(Effect calledEffect, Condition condition)
    {
        this.baseEffect = calledEffect;
        this.condition = condition;
    }
    
    public override float Call(Character caster, Character other,
                      DamageType damageType, float value)
    {
        if(condition.Check(caster, other, damageType, value))
            return baseEffect.Call(caster, other, damageType, value);
        return value;
    }
}