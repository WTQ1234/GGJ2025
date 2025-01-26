using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.LowLevel;
using HRL;

[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    public Player player;
    public PlayerOxygen playerOxygen;
    public PlayerFood playerFood;
    public BubbleData bubbleData;
    public PlayerBubbleController bubbleController;

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

    public bool isTouchBase = false;
    public bool isTouchHome = false;
    public SceneEntity_Base sceneEntity_Base;
    #endregion

    #region ======== Unity函数 ========
    public void InitData()
    {
        player.playerHealth.SetMaxHp(maxHealth);
        player.playerHealth.SetCurrentHp(maxHealth);

        playerOxygen.SetMaxHp(maxOxygen);
        playerOxygen.SetCurrentHp(maxOxygen);

        bubbleData.currentGas = bubbleData.maxGas;
        playerFood.SetMaxHp((int)bubbleData.maxGas);
        playerFood.SetCurrentHp((int)bubbleData.maxGas);
    }

    private void Start()
    {
        InitData();
        // 如果没有指定Animator，你可以在此自动获取
        if (!animator) animator = GetComponent<Animator>();

        InvokeRepeating("HandlePeriodicUpdates", 0f, 1);
    }

    private void Update()
    {
        // =========== 处理输入 ===========
        HandleSwimAndBuoyancy();
        if (!player.isDied)
        {
            HandleMovementInput();
            HandleDash();
            HandleAttack();
            HandleNetBuild();
            HandleGrab();

            // =========== 处理动画状态机参数 ===========
            UpdateAnimatorParameters();

            bubbleController.RefreshBubbleScale(bubbleData);
        }
    }

    private void HandlePeriodicUpdates()
    {
        if (!player.isDied)
        {
            // =========== 处理数值消耗与恢复 ===========
            HandleOxygenConsumption();
            HandleHealthRecoveryAndDrain();
            playerFood.RefreshHealthBarShow();
            playerOxygen.RefreshHealthBarShow();
            player.playerHealth.RefreshHealthBarShow();
        }
    }

    private void FixedUpdate()
    {
        Flip();
        // 将我们的currentVelocity应用到玩家Transform上（非物理）
        transform.Translate(currentVelocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bubble"))
        {
            Entity_Bubble entity_Bubble = collision.GetComponent<Entity_Bubble>();
            if (entity_Bubble != null)
            {
                entity_Bubble.bubbleData.buoyancyFactor *= 0.3333f;
            }
        }
        if (collision.CompareTag("Coin"))
        {
            // TODO 这里其实是吃东西 怪物的数值需要确定一下
            //SoundManager.PlayPickCoinClip();
            //LevelManager.Instance.AddExp(1);
            //CoinUI.CurrentCoinQuantity += 1;
            CoinItem coinItem = collision.GetComponent<CoinItem>();
            if (coinItem != null)
            {
                playerFood.Heal(coinItem.value);
                bubbleController.AddBubblePower(coinItem.extraName, coinItem.extraValue);
            }
            else
            {
                playerFood.Heal(5);
            }
            
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("Base"))
        {
            // TODO 基地的描边效果显示
            isTouchBase = true;
            sceneEntity_Base = collision.GetComponent<SceneEntity_Base>();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 根据Tag判断是否是气泡或空气
        if (collision.CompareTag("Bubble"))
        {
            isInBubbleRange = true;
            currentBubbleObject = collision.gameObject;
            OnDetectBubbleAir(collision.GetComponent<Entity_Bubble>());
        }
        else if (collision.CompareTag("Air"))
        {
            isInAirRange = true;
            currentAirObject = collision.gameObject;

            // 如果你想用“出水面”来瞬间补满氧气，这里可以直接做处理
            // 或者你可以根据玩家的y坐标判断是否出水面
            // 这里先简单写成：碰到空气就立即补满氧气
            playerOxygen.SetCurrentHp(maxOxygen);
            OnEnterAir();
        }
        else if (collision.CompareTag("Home"))
        {
            if (collision.gameObject.activeSelf)
            {
                isTouchHome = true;
                player.lastRespawnPoint = collision.transform;
            }
        }
        else if (collision.CompareTag("Base"))
        {
            isTouchBase = true;
            sceneEntity_Base = collision.GetComponent<SceneEntity_Base>();
            sceneEntity_Base.GetComponent<SpriteRenderer>().material.SetFloat("_Thickness", 0.01f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Bubble"))
        {
            // 离开气泡
            isInBubbleRange = false;
            currentBubbleObject = null;
            Entity_Bubble entity_Bubble = collision.GetComponent<Entity_Bubble>();
            if (entity_Bubble != null)
            {
                entity_Bubble.bubbleData.buoyancyFactor *= 3f;
            }
        }
        else if (collision.CompareTag("Air"))
        {
            // 离开空气
            isInAirRange = false;
            currentAirObject = null;
        }
        else if (collision.CompareTag("Base"))
        {
            sceneEntity_Base.GetComponent<SpriteRenderer>().material.SetFloat("_Thickness", 0f);
            isTouchBase = false;
            sceneEntity_Base = null;
        }
        else if (collision.CompareTag("Home"))
        {
            if (collision.gameObject.activeSelf)
            {

                isTouchHome = false;
            }
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

    void OnEnterAir()
    {
        float neededGas = bubbleData.maxGas - bubbleData.currentGas;
        //Debug.Log(neededGas);
        if (neededGas <= 0)
        {
            // 玩家已经满了，不做任何操作
            Debug.Log("玩家气体已满");
            return;
        }
        bubbleData.currentGas += 20 * Time.deltaTime;
        playerFood.SetCurrentHp((int)bubbleData.currentGas);
    }

    void OnDetectBubbleAir(Entity_Bubble bubble)
    {
        Debug.Log("OnDetectBubbleAir");
        Debug.Log(bubble);
        if (bubble == null)
        {
            return;
        }
        BubbleData otherBubbleData = bubble.bubbleData;
        // 1. 如果玩家的气体已达上限，则不做交互
        float neededGas = bubbleData.maxGas - bubbleData.currentGas;
        Debug.Log(neededGas);
        if (neededGas <= 0)
        {
            // 玩家已经满了，不做任何操作
            Debug.Log("玩家气体已满");
            return;
        }

        // 2. 计算气泡能提供多少（需要保留气泡的最小气体）
        float bubbleCurrentGas = bubble.GetCurrentGas();
        float bubbleMinGas = bubble.GetMinGas();
        float canGive = bubbleCurrentGas - bubbleMinGas;
        Debug.Log(canGive);
        if (canGive <= 0)
        {
            // 气泡本身也没法再提供
            return;
        }

        // 3. 确定本次实际转移的气体量
        float actualTransferred = Mathf.Min(neededGas, canGive);

        actualTransferred = Mathf.Clamp(actualTransferred, 0, 5) * Time.deltaTime;
        Debug.Log(actualTransferred);
        actualTransferred = Mathf.Clamp(actualTransferred, 0.01f, actualTransferred);
        // 4. 更新玩家气体
        bubbleData.currentGas += actualTransferred;
        playerFood.SetCurrentHp((int)bubbleData.currentGas);
        // 5. 更新气泡气体
        bubble.SetCurrentGas(bubbleCurrentGas - actualTransferred);

        if (bubble.GetCurrentGas() < 0.5f)
        {
            bubble.SetCurrentGas(0f);
        }

        // 6. 如果气泡剩余气体 <= 最小值，则销毁气泡
        if (bubble.GetCurrentGas() <= bubbleMinGas)
        {
            Destroy(bubble.gameObject);
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

        // 浮力
        force = Vector2.up * (currentBuoyancy) * player.rb2D.mass;

        // 有输入时，玩家在游动
        // 基础方向
        Vector2 swimForce = inputDirection * moveSpeed * player.rb2D.mass;

        // 将浮力对游动速度的影响加上去（可正可负）
        //swimForce += Vector2.up * currentBuoyancy * buoyancyInfluenceOnSwim * player.rb2D.mass;

        // 再考虑重力
        swimForce += Vector2.down * gravityFactor * player.rb2D.mass;

        force += swimForce;

        if (GlobalVarManager.cur_bubble_name == "Bubble_Fish")
        {
            force = swimForce * 1.4f;
        }

        player.rb2D.AddForce(force, ForceMode2D.Force);
    }

    /// <summary>
    /// 处理冲刺
    /// </summary>
    private void HandleDash()
    {
        if (GlobalVarManager.cur_bubble_name == "Bubble_Zhangyu")
        {
            return;
        }

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

            // 如果实际氧气或饱食度不够，也可以在此判断能否冲刺
            // TODO: 如果需要可在此加个判断，若不足则不冲刺
            Debug.Log(oxygenCost);
            playerOxygen.Damage(oxygenCost);

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // 冲刺方向使用 lastMoveDirection
            Vector2 dashForce = lastMoveDirection.normalized * dashSpeed * player.rb2D.mass;

            // 瞬间施加冲刺力
            player.rb2D.AddForce(dashForce, ForceMode2D.Impulse);
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
            if (isTouchBase)
            {
                if (sceneEntity_Base != null)
                {
                    if (!sceneEntity_Base.bubbleData.isFull)
                    {
                        float inputGasValue = 20 * Time.deltaTime;
                        float inputGas = inputGasValue;
                        if (bubbleData.currentGas < inputGasValue)
                        {
                            if (playerOxygen.current_value < inputGasValue)
                            {
                                inputGas = bubbleData.currentGas + playerOxygen.current_value;
                                bubbleData.currentGas = 0;
                                playerOxygen.SetCurrentHp(0);
                            }
                            else
                            {
                                inputGas = inputGasValue;
                                playerOxygen.SetCurrentHp(playerOxygen.current_value - (inputGasValue - bubbleData.currentGas));
                                bubbleData.currentGas = 0;
                            }
                        }
                        else
                        {
                            inputGas = inputGasValue;
                            bubbleData.currentGas -= inputGasValue;
                        }

                        var leftGas = sceneEntity_Base.SetBase(inputGas);
                        if (leftGas > 0)
                        {
                            bubbleData.currentGas += leftGas;
                        }
                        playerFood.SetCurrentHp((int)bubbleData.currentGas);
                    }
                }
                // TODO 将基地搞一个基地动画或者基地切帧，创建一个基地物体
            }
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
        int consumed = (int)(oxygenConsumptionRate);
        // 如果与气泡连接，则先消耗气泡氧气并补充玩家氧气
        //currentBubbleObject TODO 删掉这个
        if (bubbleData.currentGas > 0)
        {
            bubbleData.currentGas -= consumed;

            if ((!playerOxygen.isFull) && (bubbleData.currentGas > 0))
            {
                if (((playerOxygen.maxHealth - playerOxygen.current_value) > (consumed * 5)) && (bubbleData.currentGas > (consumed * 5)))
                {
                    var a = consumed * 5;
                    bubbleData.currentGas -= a;
                    playerOxygen.Heal((int)a);
                }
                else
                {
                    var a = playerOxygen.maxHealth - playerOxygen.current_value;
                    bubbleData.currentGas -= a;
                    playerOxygen.Heal((int)a);
                }
            }

            playerFood.SetCurrentHp((int)bubbleData.currentGas);
        }
        else
        {
            // 没有气泡时正常消耗玩家自己的氧气
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

    private bool isPlayerHealthy()
    {
        return (playerOxygen.current_value > (playerOxygen.maxHealth / 2));
    }

    /// <summary>
    /// 处理生命值的恢复与扣减
    /// </summary>
    private void HandleHealthRecoveryAndDrain()
    {
        if (isPlayerHealthy())
        {
            if (isTouchHome)
            {
                player.playerHealth.Heal((int)(healthRecoveryRate) * 2);
            }
            else
            {
                player.playerHealth.Heal((int)(healthRecoveryRate));
            }
        }
        // 如果氧气不足，按一定速率扣血
        if (playerOxygen.current_value <= 0)
        {
            float damageThisFrame = lowOxygenHealthDrainRate;
            TakeDamage(damageThisFrame);
        }
    }

    /// <summary>
    /// 扣血逻辑：
    /// 玩家损失本次生命 = 玩家当前生命 - 本次伤害点数 * ((玩家剩余生命百分比 + 100) / 2)
    /// </summary>
    /// <param name="damage">此次受击伤害点数</param>
    [Button("Test Take Damage")]
    public void TakeDamage(float damage)
    {
        //float currentHealthPercent = player.playerHealth.current_value / player.playerHealth.maxHealth * 100f;
        //float finalDamage = damage * ((currentHealthPercent + 1f) / 2f);
        //player.playerHealth.Damage((int)finalDamage);
        player.playerHealth.Damage((int)damage);
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
        animator.SetBool("Running", (Mathf.Abs(inputDirection.y) > 0.2f) && (inputDirection.sqrMagnitude > 0.01f));

        animator.SetBool("Run_Up", inputDirection.y > 0f);

        // 你可以添加更多参数或状态机逻辑
    }

    #endregion
}
