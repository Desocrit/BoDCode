using UnityEngine;
using System.Collections;

public class BasicAI : EnemyAI
{
    protected Character player;
    protected float rotation;

    protected bool attacking;

    // Used for measuring attack animation progress.
    protected AnimatorStateInfo stateInfo;
    protected Weapon weapon;

    public BasicAI(Enemy user) : base(user)
    {
        weapon = user.GetComponentInChildren<Weapon>();
    }

    protected override bool FindTarget()
    {
        if(player == null)
            player = GameObject.FindGameObjectWithTag("Player").
                GetComponent<Player>();
        if(player == null)
            return false;
        if(CanSeeTarget(user, player))
        {
            target = player;
            satNav.Target = target.transform;
            satNav.NavigateTarget = true;
            return true;
        }
        return false;
    }
    
    protected override void Idle()
    {
        if(user.Velocity != 0)
            user.Velocity = Mathf.Lerp(user.Velocity, 0f,
                                       Time.deltaTime * 5f);
        animator.SetFloat("Speed", user.Velocity);
    }
    
    protected override void AttackTarget()
    {
        float dist = Vector3.Distance(target.transform.position,
                                      transform.position);
        // Set speed if necessary.
        
        if(satNav.SlowToStop || dist < user.attackRange)
        {
            if(user.Velocity != 0)
                user.Velocity = Mathf.Lerp(user.Velocity, 0f,
                                           Time.deltaTime * 5f);
        } else
            if(user.Velocity != user.Stats.movementSpeed)
            user.Velocity = Mathf.Lerp(user.Velocity,
                                           user.Stats.movementSpeed,
                                           Time.deltaTime * 2.5f);

        // Face the player.
        user.GetComponent<Rigidbody>().rotation = Quaternion.identity;

        Vector3 targetPos = CanSeeTarget(user, target) ?
            target.transform.position : satNav.CurrentTargetWaypoint;

        var targetRot = Quaternion.LookRotation(targetPos - transform.position);
        targetRot = Quaternion.Inverse(targetRot) * transform.rotation;

        var angularSpd = Quaternion.Inverse(targetRot).eulerAngles.y;
        if(angularSpd > 180)
            angularSpd -= 360;

        angularVelocity = Mathf.Lerp(angularVelocity, angularSpd / 20,
                                     150f * Time.deltaTime);

        transform.rotation = Quaternion.Euler(Vector3.up * angularVelocity) *
            transform.rotation;

        animator.SetFloat("Speed", user.Velocity);

        transform.position += transform.forward *
            user.Velocity * Time.deltaTime;

        if(attacking)
        {
            if(weapon.HasNewCollisions())
                foreach(Character cha in weapon.GetCollisions())
                    user.AttackTarget(cha, 1.0f, DamageType.Physical);
            // Check collisions.
            if(!animator.GetCurrentAnimatorStateInfo(0).IsTag("attack"))
            {
                weapon.ClearList();
                attacking = false;
            }
        }

        if(dist < user.attackRange && !attacking)
        {
            animator.SetTrigger("Attack");
            attacking = true;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }
    }
}