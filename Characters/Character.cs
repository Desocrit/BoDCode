using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Character : MonoBehaviour
{

    public enum Intercept
    {
        None,
        Attack,
        DealDamage,
        CauseHealing,
        ReceiveDamage,
        ReceiveHealing,
        CastAbility,
        AbilityCooldown,
        RestoreSecondary,
        SpendSecondary,
        KillTarget,
        Die,
    }

    [SerializeField]
    protected CharacterStats
        baseStats;

    protected ModifiedStats _stats;
    public CharacterStats Stats { get { return _stats; } }
    public float Health { get { return _stats.health.Value; } }
    public float Secondary { get { return _stats.secondary.Value; } }

    public virtual float Velocity { get; set; }
    public virtual Quaternion Direction { get {
            return gameObject.transform.rotation; } }

    protected IDictionary<Intercept, List<Effect>> intercepts;
    protected System.Random rng;

    // Navigation.
    protected GridPosition gridPosition;

    // For normal construction.
    public Character(CharacterStats stats)
    {
        baseStats = stats;
        Initialise();
    }

    // For editor.
    public void Awake()
    {
        Initialise();
    }

    public virtual void Initialise()
    {
        // Add the modifier layer to the stats..
        _stats = new ModifiedStats(baseStats);

        // Set up modifiers.
        intercepts = new Dictionary<Intercept, List<Effect>>();
        
        // Initialise the RNG.
        rng = new System.Random();
    }

    public virtual void AttachGrid()
    {
        gridPosition = gameObject.GetComponent<GridPosition>();
        gridPosition.Grid = GameObject.
           FindGameObjectWithTag("GameController").transform;
    }

    public virtual void FixedUpdate()
    {
        if(Stats != null && Stats.abilities != null)
            foreach(Ability ability in Stats.abilities)
                ability.UpdateCooldown(Time.deltaTime);
    }
    
    public void OnParticleCollision(GameObject particleSystem)
    {
        // Get the root object, find the ability component, tell it to collide.
        particleSystem.transform.root.gameObject.
            GetComponent<Ability>().Collide(this);
    }

    public void AddModifier(StatModifier mod)
    {
        _stats.AddModifier(mod);
    }

    public void RemoveModifier(StatModifier mod)
    {
        _stats.RemoveModifier(mod);
    }

    public void UpdateStats()
    {
        _stats.UpdateStats();
    }
    
    public void AddEffect(Effect mod, Intercept intercept)
    {
        if(!intercepts.ContainsKey(intercept))
            intercepts[intercept] = new List<Effect>();
        intercepts[intercept].Add(mod);
        // No-intercept mods are called immediately.
        if(intercept == Intercept.None)
            mod.Call(this, this, DamageType.Physical, 1f);
    }
    
    public void RemoveEffect(Effect mod, Intercept intercept)
    {
        if(intercepts.ContainsKey(intercept))
            intercepts[intercept].Remove(mod);
    }

    // Applies damage modifiers before damage.
    public virtual void AttackTarget(Character target, float multiplier,
                                     DamageType type)
    {
        // Damage is the multiplier times base damage.
        float damage = multiplier * Stats.attackDamage;

        // Handle crit chance.
        if(rng.NextDouble() < Stats.critChance)
            damage *= Stats.critDamage;

        // Call any effects.
        if(intercepts.ContainsKey(Intercept.Attack))
            foreach(Effect mod in intercepts[Intercept.Attack])
                damage = mod.Call(this, target, type, damage);

        // Pass through the pipeline.
        DamageOrHeal(target, damage, type, true);
    }

    // Deals exact damage.
    public virtual void DamageOrHeal(Character target, float damage,
                                            DamageType type, bool modified)
    {
        // Add any extra effects here. These should not increase damage.
        Intercept intercept = (damage >= 0) ? 
            Intercept.DealDamage : Intercept.CauseHealing;

        if(intercepts.ContainsKey(intercept) && modified)
            foreach(Effect mod in intercepts[intercept])
                damage = mod.Call(this, target, type, damage);

        // Pass through the pipeline some more. Gotta love compiler inlining.
        target.ReceiveDamageOrHealing(this, damage, type, modified);
    }

    public virtual void ReceiveDamageOrHealing(Character source, float damage,
                                               DamageType type, bool modified)
    {
        // Apply Defense
        if(damage > 0 && modified)
            damage /= (type == DamageType.Physical) ?
                1 + Stats.meleeDefense : 1 + Stats.magicDefense;
        Intercept intercept = (damage >= 0) ? 
            Intercept.ReceiveDamage : 
                Intercept.ReceiveHealing;
        if(intercepts.ContainsKey(intercept) && modified)
            foreach(Effect mod in intercepts[intercept])
                damage = mod.Call(this, source, type, damage);
        _stats.health.Value = Health - damage;
        if(Health <= 0)
            Die(source);
    }

    public virtual void ModifySecondary(Character source, float amount,
                                             DamageType type,
                                             bool triggersMods)
    {
        Intercept intercept = (amount >= 0) ? 
            Intercept.SpendSecondary : 
                Intercept.RestoreSecondary;
        if(intercepts.ContainsKey(intercept) && triggersMods)
            foreach(Effect mod in intercepts[intercept])
                amount = mod.Call(this, source, type, amount);
        _stats.secondary.Value = Secondary - amount;
    }

    public virtual void RegisterKill(Character target)
    {
        // Body is empty by default.
    }

    public abstract void Die(Character killer);
}