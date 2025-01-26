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
    public Sprite sprite;

    public float scaleFactor = 1f;

    protected override void Start()
    {
        base.Start();
        sr.material = new Material(sr.sharedMaterial);
    }

    bool isDiedNow = false;
    float diedTime = 0f;
    protected override void OnDeath()
    {
        base.OnDeath();
        if (isDiedNow) {  return; }
        var item = Instantiate<CoinItem>(CoinItem, transform.position, Quaternion.identity);
        item.value = foodPower;
        item.extraName = powerName;
        item.extraValue = purePower;
        item.spriteRenderer.sprite = sprite;
        item.transform.localScale *= scaleFactor;
        isDiedNow = true;
        GetComponent<BehaviorTree>().enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        if (isDiedNow)
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

