using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ability: MonoBehaviour
{
    /* Ability Description */

    public enum AbilityType
    {
        Missile,
        Self,
        Ground
    }

    // Display name of the ability.
    public string displayName;
    // Animator trigger called.
    public string triggerCalled = null;
    public AbilityType abilityType;
    // The type of damage dealt by this ability.
    public DamageType damageType;

    // Cooldown in ms.
    public float cooldown = 0;
    // Resource cost of the ability
    public float resourceCost = 0;
    // Maximum cast range. May not be relevant. Set 0 for infinite.
    public float range = 0;
    // Does this cost health, or resources.
    public bool costsHealth = false;
    // Does this affect the caster?
    public bool affectsCaster = false;

    /* Ability Effect Description */

    public enum EffectTarget
    {
        Caster,
        Player,
        CollisionTarget
    }

    public enum EffectTime
    {
        Cast,
        FirstCollision,
        AnyCollision
    }

    [System.Serializable]
    public class AbilityEffect
    {
        public EffectTarget target;
        public EffectTime time;
        public EffectDescription description;
    }

    public AbilityEffect[] abilityEffects;

    /* Internal */
    private Character player;
    private Character caster;

    private float cooldownRemaining;

    private List<Character> targetsHit;

    // Called automatically by the editor. Instantiates effects.
    private Ability()
    {
        // This never happens. Just has to be there to compile.
        if(abilityEffects == null)
            abilityEffects = new AbilityEffect[0];
        // Find the player.
        foreach(AbilityEffect ae in abilityEffects)
        {
            if(ae.target == EffectTarget.Player)
            {
                player = null;
                break;
            }
        }
        // Instantiate some variables.
        cooldownRemaining = 0;
        targetsHit = new List<Character>();
    }

    public bool Cast(Character caster)
    {
        this.caster = caster;
        // Check ability can be casted.
        if(cooldownRemaining > 0)
            return false;
        // Adjust health/mana/cooldown values.
        if(costsHealth)
        {
            if(caster.Health < resourceCost)
                return false;
            else
                caster.DamageOrHeal(caster, resourceCost, damageType, false);
        } else
        {
            if(caster.Secondary < resourceCost)
                return false;
            else
                caster.ModifySecondary(caster, resourceCost, damageType, true);
        }
        cooldownRemaining = cooldown / caster.Stats.attackSpeed;

        // Call starting effects
        foreach(AbilityEffect ae in abilityEffects)
        {
            if(ae.time == EffectTime.Cast)
            {
                Effect effect = ae.description.GetEffect();
                effect.Call(caster, 
                            ae.target == EffectTarget.Player ? player : caster,
                            damageType, ae.description.magnitude);
            }
        }
        float offset = caster.Velocity / 4f + 0.3f;
        Vector3 castPosition = caster.transform.position + 
            (caster.Direction * Vector3.forward * offset);
        // Construct the projectile.

        GameObject graphic = null;
        ParticleSystem system = null;

        switch(abilityType)
        {
            case AbilityType.Ground:
                RaycastHit hit;
                // Raycast out.
                bool landed = false;
                if(range == 0)
                    landed = Physics.Raycast(castPosition + Vector3.up,
                                             caster.Direction * Vector3.forward,
                                             out hit);
                else
                    landed = Physics.Raycast(castPosition + Vector3.up,
                                             caster.Direction * Vector3.forward,
                                             out hit, range);
                Vector3 target = landed ? hit.point : caster.transform.position;
                // Hop up a bit and raycast down, in case we hit a wall.
                landed = Physics.Raycast(target + Vector3.up * 0.1f,
                                         Vector3.down, out hit, range);
                target = landed ? hit.point : target; 
                graphic = (GameObject)Instantiate(this.gameObject, 
                                                  target,
                                                  Quaternion.identity);
                system = ((GameObject)graphic).
                    GetComponentInChildren<ParticleSystem>();
                break;
            case AbilityType.Missile:
                graphic = (GameObject)Instantiate(this.gameObject, castPosition,
                                         caster.Direction);
                system = ((GameObject)graphic).
                    GetComponentInChildren<ParticleSystem>();
                system.startSpeed += caster.Velocity;
                break;
            case AbilityType.Self:
            default:
                graphic = (GameObject)Instantiate(this.gameObject,
                                                  caster.transform.position,
                                                  caster.transform.rotation);
                system = ((GameObject)graphic).
                    GetComponentInChildren<ParticleSystem>();
                break;
        }
        // Add destructor.
        Ability newAbility = graphic.GetComponent<Ability>();
        newAbility.caster = caster;
        newAbility.Invoke("Remove", system.duration);

        targetsHit.Clear();

        return true;
    }

    // To be called by each character object.
    public void Collide(Character collidingCharacter)
    {

        if(collidingCharacter == caster && !affectsCaster)
            return;
        if(targetsHit.Contains(collidingCharacter))
            return;
        targetsHit.Add(collidingCharacter);
        foreach(AbilityEffect ae in abilityEffects)
        {
            if(ae.time == EffectTime.AnyCollision ||
                (ae.time == EffectTime.FirstCollision && targetsHit.Count == 1))
            {
                Effect effect = ae.description.GetEffect();
                Character target;
                if(ae.target == EffectTarget.Player)
                    target = player;
                else if(ae.target == EffectTarget.Caster)
                    target = caster;
                else
                    target = collidingCharacter;
                effect.Call(caster, target, damageType,
                            ae.description.magnitude);
            }
        }

    }

    public void Remove()
    {
        // Remove any debuffs.
        foreach(AbilityEffect ae in abilityEffects)
            if(ae.description.effectType == EffectType.ApplyStatModifier)
                foreach(Character tar in targetsHit)
                    foreach(StatModifier mod in ae.description.statModifiers)
                        tar.RemoveModifier(mod);

        Destroy(gameObject);
    }

    public void UpdateCooldown(float timePassed)
    {
        if(cooldownRemaining > 0)
            cooldownRemaining -= timePassed;
    }

    // Use these for Ground only.
    public void OnTriggerEnter(Collider other)
    {
        Character tar = other.gameObject.GetComponent<Character>();
        if(tar != null && !targetsHit.Contains(tar))
            Collide(tar);
    }

    public void OnTriggerExit(Collider other)
    {
        Character tar = other.gameObject.GetComponent<Character>();
        if(tar == null || !targetsHit.Contains(tar))
            return;
        foreach(AbilityEffect ae in abilityEffects)
            if(ae.description.effectType == EffectType.ApplyStatModifier)
                foreach(StatModifier mod in ae.description.statModifiers)
                    tar.RemoveModifier(mod);
        targetsHit.Remove(tar);
    }
}