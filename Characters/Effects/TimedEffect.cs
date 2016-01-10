using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TimedEffect : Effect
{
    public float duration;
    public int repeats;

    public Effect startEffect;
    public Effect endEffect;

    private TimedEffect()
    {
    }

    public TimedEffect(float duration, Effect startEffect, Effect endEffect)
        : this(duration, 0, startEffect, endEffect)
    {
    }

    public TimedEffect(float duration, int repeats,
                       Effect startEffect, Effect endEffect)
    {
        this.startEffect = startEffect;
        this.endEffect = endEffect;
        this.duration = duration;
        this.repeats = repeats;
    }

    public override float Call(Character caster, Character other,
                      DamageType damageType, float value)
    {
        var ae = generateEffect(caster, other, damageType, value, duration,
                                repeats, startEffect, endEffect);
        return ae.EffectStart();
    }

    private ActiveEffect generateEffect(Character source, Character target, 
                                        DamageType type, float value,
                                        float duration, int repeats,
                                        Effect startEffect, Effect endEffect)
    {
        var newEffect = target.gameObject.AddComponent<ActiveEffect>();
        newEffect.source = source;
        newEffect.target = target;
        newEffect.type = type;
        newEffect.value = value;
        newEffect.duration = duration;
        newEffect.repeats = repeats;
        newEffect.startEffect = startEffect;
        newEffect.endEffect = endEffect;
        
        return newEffect;
    }

    [System.Serializable]
    public class ActiveEffect : MonoBehaviour
    {
        protected internal float duration;
        protected internal int repeats;

        protected internal Character source;
        protected internal Character target;
        protected internal DamageType type;
        protected internal float value;

        protected internal Effect startEffect;
        protected internal Effect endEffect;

        private float timer = 0;

        public float EffectStart()
        {
            if(startEffect == null)
                return value;
            return startEffect.Call(source, target, type, value);
        }
        
        public float EffectEnd()
        {
            if(endEffect == null)
                return value;
            return endEffect.Call(source, target, type, value);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if(timer >= duration)
            {
                timer = 0;
                EffectEnd();
                if(repeats > 1)
                {
                    EffectStart();
                    repeats -= 1;
                } else
                    Destroy(this);
            }
        }
    }
}