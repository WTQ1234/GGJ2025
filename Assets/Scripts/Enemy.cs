using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HRL;

public abstract class Enemy : Entity
{
    public float health;
    public float maxHealth;
    public float flashTime;

    protected Rigidbody2D self_rigidbody;

    public GameObject bloodEffect;
    public GameObject dropCoin;
    public GameObject floatPoint;

    public SpriteRenderer sr; 
    private Color originalColor;

    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        self_rigidbody = GetComponent<Rigidbody2D>();
        originalColor = sr.color;
    }

    protected virtual void OnEnable()
    {
        //health = maxHealth + LevelManager.Instance.level;
    }

    protected virtual void OnDisable()
    {

    }

    protected virtual void FixedUpdate()
    {
        _Flip();
    }

    public override void Damage(float damage = 1, bool knock_back = false, Entity trans_damage_from = null)
    {
        base.Damage(damage, knock_back, trans_damage_from);
        TakeDamage(damage, knock_back, trans_damage_from);
    }

    public void TakeDamage(float damage, bool knock_back = false, Entity trans_damage_from = null)
    {
        GameObject gb = Instantiate(floatPoint, transform.position, Quaternion.identity) as GameObject;
        gb.transform.GetChild(0).GetComponent<TextMesh>().text = damage.ToString();
        health -= damage;
        FlashColor(flashTime);
        //Instantiate(bloodEffect, transform.position, Quaternion.identity);
        //GameController.camShake.Shake();
        if (knock_back)
        {
            self_rigidbody.AddForce((trans_damage_from.transform.position - transform.position).normalized * -200);
        }

        if (health <= 0)
        {
            OnDeath();
        }
    }

    protected virtual void OnDeath()
    {
        // TODO 掉落食物
        //bool res_health = LevelManager.Instance.OnDropHealth(transform.position);
        //if (!res_health)
        //{
        //    LevelManager.Instance.OnDropExp(transform.position);
        //}
        //bool res = ObjectPoolManager.Instance.ReturnToPool(transform.name, gameObject);
        Destroy(gameObject, 2f);
        //if (!res)
        //{
        //    Destroy(gameObject);
        //}
    }

    void FlashColor(float time)
    {
        sr.color = Color.red;
        Invoke("ResetColor", time);
    }

    void ResetColor()
    {
        sr.color = originalColor;
    }

    private void _Flip()
    {
        if (self_rigidbody != null)
        {
            bool plyerHasXAxisSpeed = Mathf.Abs(self_rigidbody.velocity.x) > Mathf.Epsilon;
            if (plyerHasXAxisSpeed)
            {
                if (self_rigidbody.velocity.x < -0.1f)
                {
                    transform.localRotation = Quaternion.Euler(0, 180, 0);
                }

                if (self_rigidbody.velocity.x > 0.1f)
                {
                    transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
        }
    }
}
