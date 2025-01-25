using System;
using UnityEngine;
using UnityEngine.LowLevel;

[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    public Player player;
    public PlayerOxygen playerOxygen;
    public PlayerFood playerFood;

    #region ======== 可视化参数 ========
    [Header("基础移动参数")]
    [Tooltip("玩家的基础移动速度")]
    public float moveSpeed = 5f;
    [Tooltip("玩家冲刺速度")]
    public float dashSpeed = 12f;
    [Tooltip("玩家冲刺持续时间")]
    public float dashDuration = 0.3f;
    [Tooltip("玩家冲刺冷却时间")]
    public float dashCooldown = 1f;

    [Header("浮力与重力")]
    [Tooltip("重力系数(向下)")]
    public float gravityFactor = 1f;
    [Tooltip("浮力阈值，当浮力大于这个阈值时上浮，小于时下沉")]
    public float buoyancyThreshold = 0.1f;
    [Tooltip("移动时浮力对速度的额外修正系数（玩家游动时浮力影响减弱或加成用）")]
    public float buoyancyInfluenceOnSwim = 0.5f;

    [Header("动画相关")]
    [Tooltip("角色的Animator，用于控制动画状态机")]
    public Animator animator;
    #endregion

    #region ======== 数值相关（氧气、生命、饱食度） ========

    [Header("氧气相关")]
    [Tooltip("玩家氧气上限")]
    public int maxOxygen = 100;
    //[Tooltip("玩家当前氧气")]
    //public float currentOxygen = 100f;
    [Tooltip("玩家基础耗氧速率(每秒)")]
    public float oxygenConsumptionRate = 1f;
    [Tooltip("当玩家氧气不足时的扣血速率（每秒），会根据时间或其他条件逐渐提升，可在Update中动态修改")]
    public float lowOxygenHealthDrainRate = 2f;

    [Header("生命值相关")]
    [Tooltip("玩家最大生命值")]
    public int maxHealth = 100;
    [Tooltip("玩家生命值自动恢复速率（每秒）")]
    public float healthRecoveryRate = 0.5f;

    [Header("饱食度相关")]
    [Tooltip("玩家饱食度上限")]
    public int maxFullness = 100;
    //[Tooltip("玩家当前饱食度")]
    //public float currentFullness = 100f;
    [Tooltip("玩家基础饱食度消耗速率(每秒)")]
    public float fullnessConsumptionRate = 1f;
    [Tooltip("玩家饱食度不足时扣血速率（每秒）")]
    public float lowFullnessHealthDrainRate = 1f;
    #endregion

    #region ======== 私有变量 ========
    // 当前输入方向
    private Vector2 inputDirection;
    // 记录玩家当前的移动速度（非Rigidbody物理，而是数学计算）
    private Vector2 currentVelocity;

    // 冲刺相关
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    // 记录玩家上一次面向方向（用于冲刺朝向）
    private Vector2 lastMoveDirection = Vector2.right;

    // 气泡或空氣碰撞相关
    private bool isInBubbleRange = false;
    private bool isInAirRange = false;
    private GameObject currentBubbleObject; // 当前碰到的气泡对象
    private GameObject currentAirObject;    // 当前碰到的空气对象

    // TODO: 如果你需要区分不同的气泡或者空气对象，可以在此存更多信息，或者用列表的方式管理。
    //       比如：private List<GameObject> bubbleList = new List<GameObject>();
    //       具体看你在关卡中的需求。

    #endregion

    #region ======== Unity函数 ========
    private void Start()
    {
        player.playerHealth.SetMaxHp(maxHealth);
        player.playerHealth.SetCurrentHp(maxHealth);

        playerOxygen.SetMaxHp(maxOxygen);
        playerOxygen.SetCurrentHp(maxOxygen);

        playerFood.SetMaxHp(maxFullness);
        playerFood.SetCurrentHp(maxFullness);

        // 如果没有指定Animator，你可以在此自动获取
        if (!animator) animator = GetComponent<Animator>();

        InvokeRepeating("HandlePeriodicUpdates", 0f, 1);
    }

    private void Update()
    {
        // =========== 处理输入 ===========
        HandleMovementInput();
        HandleSwimAndBuoyancy();
        HandleDash();
        HandleAttack();
        HandleNetBuild();
        HandleGrab();

        // =========== 处理动画状态机参数 ===========
        UpdateAnimatorParameters();
    }

    private void HandlePeriodicUpdates()
    {
        // =========== 处理数值消耗与恢复 ===========
        HandleOxygenConsumption();
        HandleFullnessConsumption();
        HandleHealthRecoveryAndDrain();
    }

    private void FixedUpdate()
    {
        Flip();
        // 将我们的currentVelocity应用到玩家Transform上（非物理）
        transform.Translate(currentVelocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 根据Tag判断是否是气泡或空气
        if (collision.CompareTag("Bubble"))
        {
            isInBubbleRange = true;
            currentBubbleObject = collision.gameObject;
        }
        else if (collision.CompareTag("Air"))
        {
            isInAirRange = true;
            currentAirObject = collision.gameObject;

            // 如果你想用“出水面”来瞬间补满氧气，这里可以直接做处理
            // 或者你可以根据玩家的y坐标判断是否出水面
            // 这里先简单写成：碰到空气就立即补满氧气
            playerOxygen.SetCurrentHp(maxOxygen);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Bubble"))
        {
            // 离开气泡
            isInBubbleRange = false;
            currentBubbleObject = null;
        }
        else if (collision.CompareTag("Air"))
        {
            // 离开空气
            isInAirRange = false;
            currentAirObject = null;
        }
    }

    void Flip()
    {
        if (inputDirection.x > 0.1f)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        if (inputDirection.x < -0.1f)
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }
    #endregion

    #region ======== 输入处理 ========
    /// <summary>
    /// 处理WASD方向输入
    /// </summary>
    private void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D 或 左右方向
        float v = Input.GetAxisRaw("Vertical");   // W/S 或 上下方向
        inputDirection = new Vector2(h, v).normalized;

        // 如果玩家有移动输入，记录lastMoveDirection
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = inputDirection;
        }
    }

    /// <summary>
    /// 处理普通游动以及浮力、重力等
    /// </summary>
    private void HandleSwimAndBuoyancy()
    {
        // 获取当前浮力数据（从“气泡”或者其他系统）
        float currentBuoyancy = 0.2f;

        Vector2 force = Vector2.zero;

        //float currentBuoyancy = GetCurrentBubble().GetBuoyancyValue();
        // TODO: 目前假设GetCurrentBubble()永远不返回null，你需要自己处理是否有气泡时才获取浮力，
        //       或者给GetCurrentBubble()一个安全返回值。

        // 如果玩家没有输入方向（即静止），根据浮力和阈值决定是否上浮或下沉
        if (inputDirection.sqrMagnitude < 0.01f)
        {
            // 静止状态：纯粹受浮力与重力影响
            if (currentBuoyancy > buoyancyThreshold)
            {
                // 轻微上浮
                force = Vector2.up * (currentBuoyancy - buoyancyThreshold) * player.rb2D.mass;
            }
            else if (currentBuoyancy < -buoyancyThreshold)
            {
                // 轻微下沉（也可以加 gravityFactor 做更强下沉）
                force = Vector2.down * (Mathf.Abs(currentBuoyancy) - buoyancyThreshold) * gravityFactor * player.rb2D.mass;
            }
            // 如果浮力平衡，不需要额外的力
        }
        else
        {
            // 有输入时，玩家在游动
            // 基础方向
            Vector2 swimForce = inputDirection * moveSpeed * player.rb2D.mass;

            // 将浮力对游动速度的影响加上去（可正可负）
            swimForce += Vector2.up * currentBuoyancy * buoyancyInfluenceOnSwim * player.rb2D.mass;

            // 再考虑重力
            swimForce += Vector2.down * gravityFactor * player.rb2D.mass;

            force = swimForce;
        }

        player.rb2D.AddForce(force, ForceMode2D.Force);
    }

    /// <summary>
    /// 处理冲刺
    /// </summary>
    private void HandleDash()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                // 冲刺结束后速度恢复正常
            }
        }

        // 检测空格键触发冲刺
        if (!isDashing && dashCooldownTimer <= 0 && Input.GetKeyDown(KeyCode.Space))
        {
            // 消耗5%氧气和5%饱食度
            int oxygenCost = (int)(maxOxygen * 0.05f);
            int fullnessCost = (int)(maxFullness * 0.05f);

            // 如果实际氧气或饱食度不够，也可以在此判断能否冲刺
            // TODO: 如果需要可在此加个判断，若不足则不冲刺
            Debug.Log(oxygenCost);
            playerOxygen.Damage(oxygenCost);
            playerFood.Damage(fullnessCost);

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // 冲刺方向使用 lastMoveDirection
            currentVelocity = lastMoveDirection.normalized * dashSpeed;
        }
        else if (isDashing)
        {
            // 冲刺期间保持currentVelocity
            // 如果需要随时打断冲刺，可以在此处理
        }
    }

    /// <summary>
    /// 攻击按键（示例：鼠标左键或其他）
    /// </summary>
    private void HandleAttack()
    {
        // TODO: 你可以根据需求选择按键
        if (Input.GetMouseButtonDown(0))
        {
            // 攻击动作
            // 在动画中可以设置一个Trigger来播放攻击动画
            //animator.SetTrigger("Attack");


            
            // TODO: 攻击判定、伤害结算等
        }
    }

    /// <summary>
    /// 结网/修复网 按键（E键长按）
    /// </summary>
    private void HandleNetBuild()
    {
        // TODO: 具体的按住E键时长判断你可以在此实现
        if (Input.GetKey(KeyCode.E))
        {
            // 结网/修网动作
            // 消耗饱食度
            //int netBuildCost = 1; // 每秒消耗1点饱食度（示例）
            //playerFood.Damage(netBuildCost);
            // 这里可以考虑动画或特效
            // animator.SetBool("NetBuilding", true);
        }
        else
        {
            // animator.SetBool("NetBuilding", false);
        }
    }

    /// <summary>
    /// 抓取按键（Q键）
    /// </summary>
    private void HandleGrab()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // TODO: 触发抓取动作，判断抓到的物体类型（空气、食物、敌人、网、障碍物...）
            // - 抓到空气：获得小气泡
            // - 抓到食物：如果长按则进食
            // - 抓到敌人：对敌人扣血，若敌人死则爆食物
            // - 抓到网/障碍物：可固定自己（待定）
        }

        if (Input.GetKey(KeyCode.Q))
        {
            // TODO: 如果抓到食物，则持续进食，恢复饱食度
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            // TODO: 放开抓取，手里东西延时掉落等逻辑
        }
    }
    #endregion

    #region ======== 数值逻辑 ========

    /// <summary>
    /// 处理玩家氧气的消耗和与气泡的交互
    /// </summary>
    private void HandleOxygenConsumption()
    {
        // 如果与气泡连接，则先消耗气泡氧气并补充玩家氧气
        if (isInBubbleRange && currentBubbleObject != null)
        {
            // TODO: 需要从气泡对象拿到气泡剩余氧气(BubbleOxygen)，然后优先消耗
            //       如果气泡内氧气耗尽，则断开链接或回退到消耗自身氧气的逻辑

            // 示例：
            // float bubbleO2 = currentBubbleObject.GetComponent<Bubble>().bubbleOxygen;
            // float consumed = oxygenConsumptionRate * Time.deltaTime;
            // if (bubbleO2 > 0)
            // {
            //     // 优先从气泡中消耗
            //     float actualConsume = Mathf.Min(consumed, bubbleO2);
            //     currentBubbleObject.GetComponent<Bubble>().bubbleOxygen -= actualConsume;
            //     // 同时为玩家补充？ 或者只是不消耗玩家自己的？
            // } 
            // else 
            // {
            //     // 气泡沒氣了，正常消耗玩家自己的
            //     currentOxygen -= consumed;
            // }

            // 这里仅留空示例，你需要根据自己游戏的具体需求来补充
        }
        else
        {
            // 没有气泡时正常消耗玩家自己的氧气
            int consumed = (int)(oxygenConsumptionRate);

            // TODO: 你可以根据玩家移动速度，决定消耗更多或更少
            // 比如：如果玩家在移动，消耗翻倍
            //if (inputDirection.sqrMagnitude > 0.01f)
            //{
            //    consumed = (int)(consumed * 1.5f); // 举例：移动时额外+50%
            //}
            //Debug.Log(consumed);
            playerOxygen.Damage(consumed);
        }

        // 若玩家氧气耗尽，可在此处理逻辑
        if (playerOxygen.current_value <= 0)
        {
            // TODO: 做一些缺氧的处理，比如逐渐加快扣血
            // 也可以加一个计时器来不断提高 lowOxygenHealthDrainRate
        }

        // 当玩家浮出水面(如果用碰撞检测方式，这里就不用了；如果用坐标高度判断，可以写在此)

        if (detectAir())
        {
            Debug.Log("qucik");
            playerOxygen.Heal((int)(oxygenConsumptionRate) * 10);
        }
    }

    private bool detectAir()
    {
        // 定义检测范围
        Vector2 position = transform.position;
        float radius = 1.0f;

        // 指定 LayerMask（可同时检测多个 Layer）
        LayerMask targetLayer = LayerMask.GetMask("Air");

        // 检测范围内的碰撞体
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, radius, targetLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Air") || hitCollider.CompareTag("Bubble"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 处理饱食度的消耗
    /// </summary>
    private void HandleFullnessConsumption()
    {
        int consumed = (int)fullnessConsumptionRate;
        // 同理，移动时可能消耗更多
        //if (inputDirection.sqrMagnitude > 0.01f)
        //{
        //    consumed = (int)(consumed * 1.5f); // 示例
        //}
        playerFood.Damage(consumed);
        // TODO: 当玩家捕猎进食时才可恢复饱食度，这部分逻辑可以在HandleGrab中实现
    }

    private bool isPlayerHealthy()
    {
        return (playerOxygen.current_value > (playerOxygen.maxHealth / 2)) && (playerFood.current_value > (playerFood.maxHealth / 2));
    }

    /// <summary>
    /// 处理生命值的恢复与扣减
    /// </summary>
    private void HandleHealthRecoveryAndDrain()
    {
        if (isPlayerHealthy())
        {
            // TODO 在家里回血速度加倍
            player.playerHealth.Heal((int)(healthRecoveryRate));
        }
        // 如果氧气不足，按一定速率扣血
        if (playerOxygen.current_value <= 0)
        {
            float damageThisFrame = lowOxygenHealthDrainRate;
            TakeDamage(damageThisFrame);
        }

        // 如果饱食度不足，也要扣血
        if (playerFood.current_value <= 0)
        {
            float damageThisFrame = lowFullnessHealthDrainRate;
            TakeDamage(damageThisFrame);
        }
    }

    /// <summary>
    /// 扣血逻辑：
    /// 玩家损失本次生命 = 玩家当前生命 - 本次伤害点数 * ((玩家剩余生命百分比 + 100) / 2)
    /// </summary>
    /// <param name="damage">此次受击伤害点数</param>
    public void TakeDamage(float damage)
    {
        float currentHealthPercent = player.playerHealth.current_value / player.playerHealth.maxHealth * 100f;
        float finalDamage = damage * ((currentHealthPercent + 100f) / 2f);
        player.playerHealth.Damage((int)finalDamage);
    }

    #endregion

    #region ======== 动画状态机 ========

    /// <summary>
    /// 更新动画参数
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        // 如果有动画需求，可在此传递速度、是否游动、是否冲刺、是否攻击等给Animator
        if (!animator) return;

        // 简单示例：
        animator.SetBool("Running", inputDirection.sqrMagnitude > 0.01f);

        animator.SetBool("Run_Up", inputDirection.y > 0f);

        // 你可以添加更多参数或状态机逻辑
    }

    #endregion

    #region ======== 气泡系统(示例) ========
    /// <summary>
    /// 获取当前与玩家关联的气泡信息
    /// 如果没有气泡，最好返回一个安全的默认值
    /// </summary>
    private Bubble GetCurrentBubble()
    {
        // TODO: 这是一个示例，你需要根据实际情况去获取真正的气泡
        //       如果isInBubbleRange为false，或者气泡对象为空，可返回一个默认Bubble
        if (currentBubbleObject != null)
        {
            Bubble b = currentBubbleObject.GetComponent<Bubble>();
            if (b != null)
            {
                return b;
            }
        }
        // 如果没有检测到气泡，就返回一个默认空实现
        return new Bubble();
    }
    #endregion
}

/// <summary>
/// 一个示例Bubble类，仅提供一个浮力值，供GetCurrentBubble()使用
/// 你可以根据自己的需求进一步扩展（包括气泡氧气量等）
/// </summary>
public class Bubble : MonoBehaviour
{
    [Tooltip("该气泡提供的浮力大小")]
    public float buoyancyValue = 0.2f;

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
}
