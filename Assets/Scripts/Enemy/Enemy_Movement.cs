using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Movement : MonoBehaviour
{
    public static Enemy_Movement instance;

    public float speed;
    public float attackRange = 1.2f;//攻击范围，玩家靠近多近才会发起攻击的范围
    public float attackCooldown = 2;//攻击冷却
    //本质上攻击冷却就是个进度条，满了是2，倒计时结束为零攻击，攻击结束后重新回2，也就是说0为完成冷却可以攻击，2为冷却未完成

    public float playerDetectRange = 5;//玩家检测距离（圆形探测范围的半径）

    [Header("巡逻设置")]
    public float patrolRange = 3;//巡逻半径：敌人以出生点为中心，在这个圆形范围内漫游
    public float patrolSpeed = 2;//巡逻移动速度（比追击慢，显得悠闲）
    public float patrolWaitTime = 1.5f;//到达一个巡逻点后停留的时间

    [Header("攻击命中延迟（从进入 Attacking 状态到实际判定伤害的时间）")]
    public float attackHitDelay = 0.35f;

    [Header("攻击收招时间（判定伤害后、退出攻击状态前等待的时间，用来把攻击动画播完）")]
    public float attackRecoveryTime = 0.25f;

    public Transform detectionPoint;//检测点
    public LayerMask playerLayer;//玩家层级

    private float attackCooldownTimer;//攻击冷却倒计时
    private int facingDirection = -1;//初始面向方向，主要是用来控制动画
    public EnemyState enemyState;
    private Rigidbody2D rb;
    private Transform player;
    private Animator anim;

    private Vector2 patrolCenter;//巡逻中心：记录出生位置
    private Vector2 patrolTarget;//当前巡逻目标点
    private float patrolWaitTimer;//巡逻停留倒计时
    private float stuckCheckTimer;//卡墙检测计时器：每0.5秒核对一次实际位移
    private Vector2 lastCheckPos;//上次卡墙检测时记录的位置


    private void Awake()
    {
        instance = this;
    }
    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        patrolCenter = transform.position;//以出生点作为巡逻中心
        PickNewPatrolTarget();
        ChangeState(EnemyState.Patrolling);//开局直接进入巡逻状态
    }

    void Update()
    {
        //Knockback（旧击退）、Hunt（受击）、Dead（死亡）三种状态下都暂停检测和追击
        if (enemyState != EnemyState.Knockback && enemyState != EnemyState.Hunt && enemyState != EnemyState.Dead)
        {
            CheckForPlayer();//检查玩家
            if (attackCooldownTimer > 0)//如果冷却效果在生效
            {
                attackCooldownTimer -= Time.deltaTime;
            }
            if (enemyState == EnemyState.Chasing)
            {
                Chase();
            }
            else if (enemyState == EnemyState.Attacking)
            {
                rb.velocity = Vector2.zero;
            }
            else if (enemyState == EnemyState.Patrolling)
            {
                Patrol();
            }
        }
    }
    void Chase()//追击方法
    {
        if (player.position.x > transform.position.x && facingDirection == -1 || player.position.x < transform.position.x && facingDirection == 1) //如果玩家在敌人右边，敌人却面向左
        {
            Flip();
        }
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * speed;
    }

    void Patrol()//巡逻：朝随机目标点移动，到达后停留一会再换下一个点
    {
        if (patrolWaitTimer > 0)//停留阶段：原地等待倒计时
        {
            rb.velocity = Vector2.zero;
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
                PickNewPatrolTarget();//停留结束，前往下一个随机巡逻点
            return;
        }

        Vector2 direction = (patrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = direction * patrolSpeed;

        //根据移动方向翻转朝向
        if (direction.x > 0 && facingDirection == -1 || direction.x < 0 && facingDirection == 1)
            Flip();

        //到达检测：足够接近目标点就算到达，进入停留（没有这个判断敌人只会走向第一个点然后停死）
        if (Vector2.Distance(transform.position, patrolTarget) <= 0.2f)
        {
            rb.velocity = Vector2.zero;
            patrolWaitTimer = patrolWaitTime;
            return;
        }

        //卡墙检测：每0.5秒对比一次"理论应走距离"和"实际位移"
        //实际位移远小于理论值说明被墙挡住了，立刻换一个新方向，不再持续撞墙
        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= 0.5f)
        {
            float expected = patrolSpeed * stuckCheckTimer;//0.5秒理论应走的距离
            float actual = Vector2.Distance(transform.position, lastCheckPos);//实际移动了多少

            if (actual < expected * 0.25f)
                PickUnstuckTarget();

            stuckCheckTimer = 0f;
            lastCheckPos = transform.position;
        }
    }

    void PickNewPatrolTarget()//以出生点为圆心的圆形范围内随机取一个巡逻目标点
    {
        patrolTarget = patrolCenter + Random.insideUnitCircle * patrolRange;
        stuckCheckTimer = 0f;
        lastCheckPos = transform.position;
    }

    void PickUnstuckTarget()//被墙挡住时换目标：新方向至少偏离当前方向90度，避免继续撞同一面墙
    {
        Vector2 currentDir = (patrolTarget - (Vector2)transform.position).normalized;
        //在90~270度之间随机转向，保证不会再朝着刚撞过的那面墙走
        Vector2 newDir = Quaternion.Euler(0, 0, Random.Range(90f, 270f)) * currentDir;
        Vector2 target = (Vector2)transform.position + newDir * Random.Range(1f, patrolRange);

        //把新目标点拉回巡逻圈内，防止越走越远
        if (Vector2.Distance(target, patrolCenter) > patrolRange)
            target = patrolCenter + (target - patrolCenter).normalized * patrolRange;

        patrolTarget = target;
        stuckCheckTimer = 0f;
        lastCheckPos = transform.position;
    }

    private void CheckForPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectionPoint.position,playerDetectRange,playerLayer);

        if (hits.Length > 0)
        {
            player = hits[0].transform;
            //检查该玩家是否在攻击范围内以及是否冷却完成
            if (Vector2.Distance(transform.position, player.position) <= attackRange && attackCooldownTimer <= 0)//如果玩家在敌人的敌人的攻击范围
            {
                attackCooldownTimer = attackCooldown;
                ChangeState(EnemyState.Attacking);
            }
            else if(Vector2.Distance(transform.position, player.position) > attackRange && enemyState != EnemyState.Attacking)
            {
                ChangeState(EnemyState.Chasing);
            }
        }
        else
        {
            //玩家不在圆形检测范围内：回到巡逻状态继续漫游（不再原地罚站）
            if (enemyState != EnemyState.Patrolling)
                ChangeState(EnemyState.Patrolling);
        }
    }

    private void Flip()
    {
        facingDirection *= -1;
        transform.localScale =new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    public void ChangeState(EnemyState newState)
    {
        enemyState = newState;//无论动画是否正常，状态必须切换

        //进入 Attacking 状态时，启动攻击协程（替代 Animation Event，所有敌人统一生效）
        if (newState == EnemyState.Attacking)
            StartCoroutine(DoAttackSequence());

        if (anim == null) return;

        //每次切换把所有布尔一次性归位，只有当前状态对应的为true
        //旧写法只清"上一个状态"的布尔，一旦某次没对上就会同时有两个布尔为true，
        //入口连线按顺序判断永远先进第一个匹配的，动画就会卡死（比如一直停在受击）
        anim.SetBool("isIdle", enemyState == EnemyState.Idle);
        //巡逻复用追击的走路动画（isChasing），无需在Animator里新增参数
        anim.SetBool("isChasing", enemyState == EnemyState.Chasing || enemyState == EnemyState.Patrolling);
        anim.SetBool("isAttacking", enemyState == EnemyState.Attacking);
        anim.SetBool("isHunt", enemyState == EnemyState.Hunt);
        anim.SetBool("isDead", enemyState == EnemyState.Dead);
    }

    //协程：进入 Attacking 后延迟 attackHitDelay 秒判定伤害，再等攻击动画播完后退出攻击状态
    //关键：攻击结束后必须主动切回 Idle。否则敌人会一直停在 Attacking 状态——
    //卡在攻击动画最后一帧不动，冷却结束后又在原地反复对靠近的玩家造成伤害（原来的 bug）
    private System.Collections.IEnumerator DoAttackSequence()
    {
        //延迟对齐挥刀动画的"接触帧"，让伤害看起来和动画同步
        yield return new WaitForSeconds(attackHitDelay);

        //状态已变（比如被击退/死亡）就不再判定伤害
        if (enemyState != EnemyState.Attacking) yield break;

        Enemy_Combat combat = GetComponent<Enemy_Combat>();
        if (combat != null)
            combat.Attack();
        else
            Debug.LogWarning($"{name} 没有 Enemy_Combat 组件，无法造成伤害", this);

        //等攻击动画播完（收招阶段）
        yield return new WaitForSeconds(attackRecoveryTime);

        //攻击收招结束，退出攻击状态回到待机；之后冷却结束、玩家还在范围内会重新攻击
        if (enemyState == EnemyState.Attacking)
            ChangeState(EnemyState.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPoint.position, playerDetectRange);
        Gizmos.color = Color.yellow;//黄色圈：巡逻漫游范围
        Gizmos.DrawWireSphere(transform.position, patrolRange);
    }
}

public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Knockback,
    Hunt,
    Dead,
}
