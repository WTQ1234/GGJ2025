using HRL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAttack : MonoBehaviour
{
    public Entity Owner;
    public PlayerAbility_Attack playerAbility_Attack;
    public Collider2D collider2d;
    public Animator animator;

    public void StartAttackDetect()
    {
        if (GlobalVarManager.cur_bubble_name != "Bubble_Kedou")
        {
            return;
        }
        // TODO 播放刀气动画
        collider2d.enabled = true;
    }

    public void EndAttackDetect()
    {
        collider2d.enabled = false;
        animator.ResetTrigger("Attack");
    }

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collider2d.enabled) return;
        if (collision.tag == "SceneEntity")
        {
            SceneEntity sceneEntity = collision.GetComponent<SceneEntity>();
            // TODO: 其实SceneEntity也可以考虑统合到team里
            if (sceneEntity == null) return;
            _CauseDamage(sceneEntity);
        }
        else if (collision.tag == "Player")
        {
            Entity entity = collision.GetComponent<Entity>();
            if (entity == null) return;
            TeamController teamController = entity.teamController;
            int teamId = teamController.teamId;
            if (!Owner.teamController.DetectTeam(teamId))
            {
                _CauseDamage(entity);
            }
        }
        else if (collision.tag == "Enemy")
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            enemy.TakeDamage(5, true, Owner);
        }
    }

    protected void _CauseDamage(Entity target)
    {
        MobAttrBase attr_atk = Owner.attrController.GetAttr<MobAttrBase>("Damage");
        MobAttrBase attr_defense = Owner.attrController.GetAttr<MobAttrBase>("Defense");
        if (attr_atk != null)
        {
            if (attr_defense != null)
            {
                target.Damage(attr_atk.current_value - attr_defense.current_value, true, Owner);
            }
            else
            {
                target.Damage(attr_atk.current_value, true, Owner);
            }
        }
        else
        {
            target.Damage(5, true, Owner);
        }
    }
}
