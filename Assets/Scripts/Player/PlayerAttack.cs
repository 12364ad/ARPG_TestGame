using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerAttack : MonoBehaviour
{


    public Transform attackPoint;
    public float weaponRange = 1;
    public LayerMask enemyLayer;
    public LayerMask destructibleLayer;//可破坏物品图层
    public float attackPointDistance = 1f;   //攻击点离玩家中心的距离，可在 Inspector 调
    [Header("攻击扇形角度")]
    [Tooltip("90° 表示前方 ±45° 的扇形区域才能被击中，360° 等于全向")]
    public float attackAngle = 90f;          //攻击扇形的总角度（度）
    private Vector2 attackDirection;         //攻击方向（水平/垂直）
    //伤害不再写死在这里，由 StatsManager.finalAD 提供（基础攻击力 + 装备加成）

    public static PlayerAttack Instance;
    private Animator anim;
    public float cooldown = 2;//冷却时间
    private float timer;//冷却倒计时

    public bool attacking = false;//正在攻击
    public PlayerMove playerMove;
    private bool damageDealt;//本次攻击是否已结算伤害（防止动画重播导致重复扣血）


    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        AttackDirection();

        //每帧用世界坐标同步攻击点 = 玩家位置 + 攻击方向 × 距离
        //用世界坐标赋值不受父物体 Sprite 翻转镜像的影响，方向永远正确
        if (attackPoint != null)
            attackPoint.position = transform.position + (Vector3)(attackDirection * attackPointDistance);

        if (timer > 0)
            {
            timer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.K) && !attacking && (PlayerMove.Instance == null || !PlayerMove.Instance.isDead))
        {
            if(Attack())   //只有真正发起攻击时才锁定移动
                StartCoroutine(EndAttackAfterAnimation());
        }

    }

    public void AttackAnimationFinished()
    {
        attacking = false;  //保留动画事件作为兜底（如果配了也能用）

    }

    public bool Attack()
    {
        if(timer > 0) return false;   //冷却中 → 不发起攻击，也不锁定状态

        damageDealt = false;          //新攻击开始，允许结算一次伤害
        //攻击点位置已由 Update 每帧用世界坐标同步，这里不再重复设置
        anim.SetTrigger("Attack");
        timer = cooldown;

        // 挥砍音效（每次攻击只播放一次，不管有没有命中）
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxSlash);

        return true;
    }
    public void DealDamage()//造成伤害
    {
        if (damageDealt) return;      //本次攻击已结算过 → 动画重播的重复触发，直接跳过
        damageDealt = true;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, weaponRange, enemyLayer);//以attackPoint为原点，weaponRange为范围，enemyLayer为目标
        Collider2D[] destructiveHitColliders = Physics2D.OverlapCircleAll(attackPoint.position, weaponRange, destructibleLayer);
        //Debug.Log($"DealDamage 触发 | 检测到 {enemies.Length} 个目标 | 攻击点位置 {attackPoint.position} ");
        //判断是否为敌人
        float cosHalfAngle = Mathf.Cos(attackAngle * Mathf.Deg2Rad / 2f); // 预计算扇形半角余弦，避免循环里重复算
        bool hitAny = false;
        foreach (Collider2D enemy in enemies)   //遍历所有目标，找到带血量组件的那个
        {
            //GetComponentInParent：Collider2D 可能在敌人的子物体（如 AttackPoint）上，
            //Enemy_Health / Enemy_Knockback 挂在根 GameObject 上，必须往父物体找
            Enemy_Health health = enemy.GetComponentInParent<Enemy_Health>();
            if (health == null)                     //跳过没有血量组件的碰撞体（如武器、特效）
                continue;

            // 扇形过滤：敌人必须在攻击方向 ±attackAngle/2 的扇形内才命中
            // 用点积判断：两个单位向量的点积 = cos(夹角)，夹角越小越接近 1
            Vector2 enemyDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Dot(enemyDir, attackDirection) < cosHalfAngle)
                continue;

            hitAny = true;
            int damage = StatsManager.instance.finalAD;
            health.ChangeHealth(-damage);
            Debug.Log($"对 {enemy.name} 造成 {damage} 伤害");

            Enemy_Knockback knockback = enemy.GetComponentInParent<Enemy_Knockback>();
            if (knockback != null)
                knockback.Knockback(transform, StatsManager.instance.knockbackForce);
            else
                Debug.LogWarning($"对 {enemy.name} 造成伤害但没找到 Enemy_Knockback 组件（无击退）");
        }

        // 击中音效：只要命中至少一个敌人就播放一次（不按敌人数叠加，避免太吵）
        if (hitAny)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxImpact);
        //判断是否为可破坏物体
        foreach(Collider2D hitCollider in destructiveHitColliders)
        {
            hitCollider.GetComponent<Destructible>().DestroyObject();//破坏物品
        }
    }
    public void AttackDirection()
    {
        //用最后移动方向（lastMove）而不是实时输入：站立攻击时也能保持朝向，不会归零
        Vector2 dir = PlayerMove.Instance.lastMove;

        //四方向：取绝对值大的那个轴，把另一个清零
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            attackDirection = new Vector2(Mathf.Sign(dir.x), 0);   //水平为主 → 左/右
        else
            attackDirection = new Vector2(0, Mathf.Sign(dir.y));   //垂直为主 → 上/下

        //游戏刚开始还没动过 → 默认朝右
        if (attackDirection == Vector2.zero)
            attackDirection = Vector2.right;
    }

    /// <summary>
    /// 攻击动画开始后，等动画播完自动把 attacking 重置为 false，
    /// 不再依赖 Animation Event，避免漏配导致永久卡死。
    /// </summary>
    IEnumerator EndAttackAfterAnimation()
    {
        attacking = true;
        // 等一帧，让 Animator 真正进入攻击状态
        yield return new WaitForEndOfFrame();
        // 拿到当前攻击动画的实际时长
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
        attacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, weaponRange);
    }
}
