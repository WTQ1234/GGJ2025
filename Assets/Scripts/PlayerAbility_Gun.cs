using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HRL;

public class PlayerAbility_Gun : MobAbilityBase
{
    [HideInInspector]
    public Weapon weapon;
    public Animator animator;

    public Transform shoot_point;

    public int damage = 1;
    public float bullet_volicity = 1;

    public int each_bullet_num = 1;
    public float aim_angle = 0f;

    public Bullet prefab_bullet;

    // 这个是要消耗的魔法值或者什么
    public PlayerHealth attr;

    protected override void _SetName()
    {
        Name = "Gun";
    }

    protected override bool _Execute()
    {
        base._Execute();
        animator.SetBool("Attack", true);
        if (invokeByLongPress)
        {
            isCurrentLongPress = true;
        }
        OnShoot();
        return true;
    }

    protected override void _Cancel()
    {
        base._Cancel();
        animator.SetBool("Attack", false);
        if (invokeByLongPress)
        {
            isCurrentLongPress = false;
        }
    }

    public void OnShoot()
    {
        Debug.Log("on shoot");
        if (!this.enabled)
        {
            return;
        }
        if (attr.current_value <= 0)
        {
            return;
        }
        //if (currentTarget == null)
        //{
        //    return;
        //}
        bool isShotgunMode = (GlobalVarManager.cur_bubble_name == "Bubble_Kedou");
        int bulletCount = isShotgunMode ? Random.Range(3, 6) : each_bullet_num; // 根据模式决定子弹数量
        float spreadAngle = isShotgunMode ? 45 : aim_angle; // 根据模式决定散射角度
        float velocityMultiplier = isShotgunMode ? 0.5f : 1f; // 根据模式决定速度倍率
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPosition - shoot_point.position).normalized;

        for (int i = 0; i < bulletCount; i++)
        {
            Bullet bullet = Instantiate<Bullet>(prefab_bullet);
            bullet.transform.position = shoot_point.position;
            bullet.OnSetDamage(damage);
            //bullet.teamController.SetTeam(Owner.teamController);

            if (spreadAngle > 0)
            {
                float cur_angle = Random.Range(-spreadAngle, spreadAngle);
                bullet.transform.rotation = shoot_point.rotation;
                bullet.transform.Rotate(Vector3.forward * cur_angle);
                Matrix4x4 rotate = Matrix4x4.Rotate(Quaternion.Euler(0, 0, cur_angle));
                bullet.OnSetVelocity(rotate.MultiplyVector(direction), bullet_volicity * velocityMultiplier);
            }
            else
            {
                bullet.transform.rotation = shoot_point.rotation;
                bullet.OnSetVelocity(direction, bullet_volicity * velocityMultiplier);
            }
        }
        if (GlobalVarManager.cur_bubble_name == "Bubble_Bug")
        {
            attr?.Damage(bulletCount);
        }
        else
        {
            attr?.Damage(bulletCount * 2);
        }
        weapon?.OnCostDurable(1);
    }
}
