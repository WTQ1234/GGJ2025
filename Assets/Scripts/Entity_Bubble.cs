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
    [SerializeField]
    public BubbleData bubbleData = new BubbleData();

    private Rigidbody2D rb;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 确保Collider2D是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        // 根据当前气体计算浮力，并向上施加力
        float upwardForce = bubbleData.buoyancyFactor * bubbleData.currentGas;
        rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);
    }

    private void Update()
    {
        // 根据当前气体，改变气泡大小（示例：从 minGas->maxGas 做一个插值，或简单按比例放缩）
        float scalePercent = bubbleData.currentGas / bubbleData.maxGas;
        scalePercent = Mathf.Clamp01(scalePercent);  // 0~1之间
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1.0f, scalePercent);

        // 如果被吸收者气体低于或等于最小值，则销毁
        if (bubbleData.currentGas <= bubbleData.minGas)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 如果碰到Tag为"Air"的物体，立即消失
        if (other.CompareTag("Air"))
        {
            Destroy(gameObject);
            return;
        }

        // 2. 与其他Bubble相遇时，较大者吸收较小者气体
        Entity_Bubble otherBubble = other.GetComponent<Entity_Bubble>();
        if (otherBubble != null && otherBubble != this)
        {
            float myGas = bubbleData.currentGas;
            float otherGas = otherBubble.bubbleData.currentGas;

            // 如果两者气体正好相等，这里可以选择“不做处理”或“互相不变”等逻辑。
            // 下面示例中仅当两者不相等时才触发吸收。
            if (Mathf.Approximately(myGas, otherGas)) return;

            if (myGas > otherGas)
            {
                Absorb(otherBubble);
            }
            else
            {
                otherBubble.Absorb(this);
            }
        }
    }


    /// <summary>
    /// 吸收另一个气泡的气体
    /// </summary>
    /// <param name="smallerBubble">被吸收气泡（气体更小）</param>
    public void Absorb(Entity_Bubble smallerBubble)
    {
        float neededGas = bubbleData.maxGas - bubbleData.currentGas;
        // 如果自身已满，则不再吸收
        if (neededGas <= 0) return;

        // 对方可提供的气体量（需要保留它的最小气体下限）
        float canGive = smallerBubble.bubbleData.currentGas - smallerBubble.bubbleData.minGas;
        if (canGive <= 0) return;

        // 实际吸收量
        float actualTransfer = Mathf.Min(neededGas, canGive);

        // 更新自身气体
        bubbleData.currentGas += actualTransfer;
        // 更新被吸收者气体
        smallerBubble.bubbleData.currentGas -= actualTransfer;
    }

    /// <summary>
    /// 提供给Player脚本等外部访问的Get/Set方法（可选）
    /// </summary>
    public float GetCurrentGas()
    {
        return bubbleData.currentGas;
    }

    public float GetMaxGas()
    {
        return bubbleData.maxGas;
    }

    public float GetMinGas()
    {
        return bubbleData.minGas;
    }

    public void SetCurrentGas(float newGas)
    {
        bubbleData.currentGas = newGas;
    }

    private Vector2 currentVelocity;
}

[System.Serializable]
public class BubbleData
{
    [Header("气体相关")]
    public float currentGas = 5f;  // 当前气体
    public float maxGas = 10f;     // 气體上限
    public float minGas = 1f;      // 氣體下限

    [Header("浮力相关")]
    public float buoyancyFactor = 0.2f;  // 浮力系数(可以调整浮力大小)

    public bool isFull
    {
        get
        {
            return currentGas >= maxGas;
        }
    }

    public float GetBuoyancyValue()
    {
        // 你可以进行一些动态计算或衰减，这里只是返回一个固定值
        return buoyancyFactor;
    }

    public float TryAddGas(float curGas)
    {
        currentGas += curGas;
        if (curGas > maxGas)
        {
            float gas = curGas - maxGas;
            curGas = maxGas;
            return gas;
        }
        return 0;
    }
}