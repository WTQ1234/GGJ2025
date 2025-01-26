using HRL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFood : PlayerHealth {
    public PlayerAbility_Gun playerAbility_Gun;
    public PlayerController2D playerController2D;
    private void Start()
    {
        playerAbility_Gun.attr = this;
    }

    public override void Damage(float damage, bool knock_back = false, Entity trans_damage_from = null)
    {
        //base.Damage(damage, knock_back, trans_damage_from);
        if (playerController2D.bubbleData.currentGas > damage)
        {
            playerController2D.bubbleData.currentGas -= damage;
        }
        else
        {
            playerController2D.bubbleData.currentGas = 0;
        }
        
        SetCurrentHp((int)playerController2D.bubbleData.currentGas);
    }
}
