using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks.Unity.Math;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class EnemySmartBat : Enemy
{
    public CoinItem CoinItem;

    public string powerName;
    public int foodPower;
    public float purePower;

    protected override void Start()
    {
        base.Start();
        sr.material = new Material(sr.sharedMaterial);
    }

    bool isDied = false;
    float diedTime = 0f;
    protected override void OnDeath()
    {
        base.OnDeath();
        var item = Instantiate<CoinItem>(CoinItem, transform.position, Quaternion.identity);
        item.value = foodPower;
        item.extraName = powerName;
        item.extraValue = purePower;
        isDied = true;
        GetComponent<BehaviorTree>().enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        if (isDied)
        {
            diedTime += Time.deltaTime;
            transform.Translate(0, Time.deltaTime * 0.5f, 0);
            sr.material.SetFloat("_Fade", Mathf.Lerp(1, 0, diedTime / 1));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Home"))
        {
            ccTime = 0;
        }
    }

    float ccTime;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Home"))
        {
            ccTime += Time.deltaTime;
            if (ccTime > 1)
            {
                TakeDamage(1);
                ccTime = 0;
            }
            
            self_rigidbody.AddForce((collision.transform.position - transform.position).normalized * 7);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        
    }
}

