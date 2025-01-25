using HRL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个示例Bubble类，仅提供一个浮力值，供GetCurrentBubble()使用
/// 你可以根据自己的需求进一步扩展（包括气泡氧气量等）
/// </summary>
public class Entity_Bubble : Entity
{
    [Tooltip("该气泡提供的浮力大小")]
    public float buoyancyValue = 0.2f;
    [Tooltip("浮力阈值，当浮力大于这个阈值时上浮，小于时下沉")]
    public float buoyancyThreshold = 0.1f;

    // TODO: 如果你有气泡氧气量、气泡生成、气泡消失等逻辑，可在此扩展
    // public float bubbleOxygen = 50f; etc...

    /// <summary>
    /// 获取当前气泡提供的浮力
    /// </summary>
    public float GetBuoyancyValue()
    {
        // 你可以进行一些动态计算或衰减，这里只是返回一个固定值
        return buoyancyValue;
    }

    private Vector2 currentVelocity;

    private void FixedUpdate()
    {
        //Flip();
        // 将我们的currentVelocity应用到玩家Transform上（非物理）
        transform.Translate(currentVelocity * Time.fixedDeltaTime);
    }

    protected override void Update()
    {
        float currentBuoyancy = 0.2f;
        // 静止状态：纯粹受浮力与重力影响
        if (currentBuoyancy > buoyancyThreshold)
        {
            // 轻微上浮
            currentVelocity = Vector2.up * (currentBuoyancy - buoyancyThreshold);
        }
        else if (currentBuoyancy < -buoyancyThreshold)
        {
            // 轻微下沉（也可以加 gravityFactor 做更强下沉）
            currentVelocity = Vector2.down * (Mathf.Abs(currentBuoyancy) - buoyancyThreshold) * 1;
        }
        else
        {
            // 浮力接近平衡，不动
            currentVelocity = Vector2.zero;
        }
    }
}
