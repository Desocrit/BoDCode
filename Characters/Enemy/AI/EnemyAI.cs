using UnityEngine;
using System.Collections;

public abstract class EnemyAI
{
    protected Enemy user;
    protected Navigator satNav;
    protected Character target;
    protected float angularVelocity;

    protected Transform transform;
    protected Animator animator;

    public EnemyAI(Enemy user)
    {
        this.user = user;
        satNav = user.gameObject.GetComponent<Navigator>();

        animator = user.GetComponentInChildren<Animator>();
        transform = user.transform;

        animator.SetFloat("Speed", 0f);
        satNav.ChangeNodeDist = 0.3f;
        satNav.SlowToStopDist = user.attackRange;
    }

    public virtual void PerformActions()
    {
        // First, check if target is lost or acquired (i.e. update percepts)
        if(target == null)
            FindTarget();
        else if(Vector3.Distance(target.transform.position,
                                   transform.position) > user.sightRange)
            target = null;

        // Next, act on this information.
        if(target == null)
            Idle();
        else
            AttackTarget();
    }

    protected abstract bool FindTarget();

    protected abstract void Idle();

    protected abstract void AttackTarget();

    public void SetTarget(Character target)
    {
        this.target = target;
    }

    public static bool CanSeeTarget(Enemy user, Character target)
    {
        var transform = user.transform;

        // Find the angle between this and the target.
        Vector3 direction = target.transform.position - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);

        if(angle < user.fieldOfViewAngle * 0.5f)
        {
            RaycastHit hit;
            if(Physics.Raycast(transform.position + transform.up * 1f,
                               direction.normalized, out hit, user.sightRange))
                return hit.collider.gameObject == target.gameObject;
        }

        return false;
    }

    public static BasicAI GetAI(AIType type, Enemy user)
    {
        switch(type)
        {
            case AIType.BasicMelee:
                return new BasicAI(user);
            default:
                return null;
        }
    }
}