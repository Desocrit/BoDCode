using UnityEngine;
using System.Collections;

public class Enemy : Character
{
    public AIType aiType;

    public float attackRange;
    public float sightRange;
    public float fieldOfViewAngle;

    [System.NonSerialized]
    public Animator
        animator;

    protected EnemyAI ai;

    protected bool dead = false;
    protected bool fadeOut = false;

    public Enemy(CharacterStats stats, AIType aiType) : base(stats)
    {
        this.aiType = aiType;
        Initialise();
    }

    public override void Initialise()
    {
        base.Initialise();
        animator = GetComponent<Animator>();
        ai = EnemyAI.GetAI(aiType, this);
    }

    public override void FixedUpdate()
    {
        if(Health > 0)
        {
            base.FixedUpdate();
            ai.PerformActions();
        } else if(fadeOut)
        {
            foreach(Renderer renderer in
                    GetComponentsInChildren<Renderer>())
            {
                Color colour = renderer.material.color;
                if(colour.a > 0f)
                    colour.a -= 0.01f;
                renderer.material.color = colour;
            }
        }
    }

    public override void AttachGrid()
    {
        base.AttachGrid();
        gameObject.GetComponent<Navigator>().Grid = GameObject.
            FindGameObjectWithTag("GameController").transform;
    }

    public override void ReceiveDamageOrHealing(Character source, float damage,
                                               DamageType type, bool modified)
    {
        base.ReceiveDamageOrHealing(source, damage, type, modified);
        if(damage > 0 && source.Stats.alliance != Stats.alliance
            && source.Stats.alliance != CharacterStats.Alliance.Immune)
            ai.SetTarget(source);
    }

    public override void Die(Character killer)
    {
        if(dead)
            return;
        dead = true;
        animator.SetTrigger("Dead");

        killer.RegisterKill(this);
        Invoke("BeginFade", 28f);
        // Play a death animation.
        Destroy(gameObject, 30f);
    }

    private void BeginFade()
    {
        fadeOut = true;
    }
}