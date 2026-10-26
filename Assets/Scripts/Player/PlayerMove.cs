using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour//玩家移动
{
    public static PlayerMove Instance;

    private Rigidbody2D rb;//刚体组件引用
    private Animator anim;
    [Header("玩家属性")]
    public float speed = 5;//玩家速度
    [Header("水平轴")]
    [SerializeField] public float horizontal;
    [Header("垂直轴")]
    [SerializeField] public float vertical;
    [SerializeField] private Vector2 movementInput;//移动输入
    [Header("最后一次移动")]
    [SerializeField] public Vector2 lastMove;//最后一次移动（攻击方向用）

    private bool isKnockedBack;

    [HideInInspector] public bool isDead;//死亡标志：由StatsManager在死亡/复活时控制

    private void Awake()
    {
        //单例守卫：切图时若新场景里意外存在另一个 PlayerMove，销毁重复的
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //跨场景保留，配合 SceneTransitionTrigger 切图后继续操作同一个玩家
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
    }

    private void Update()//在Update中检测用户输入
    {
        if (isDead)//死亡期间屏蔽所有移动输入
        {
            horizontal = 0;
            vertical = 0;
            movementInput = Vector2.zero;
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");//用来获取某项输入-1,0,1
        vertical = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(horizontal, vertical);
    }

    private void FixedUpdate()//在FixedUpdate中做物理运动控制
    {
        if (isDead)//死亡期间原地定住、保持待机
        {
            rb.velocity = Vector2.zero;
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        if(isKnockedBack == false)
        {
            if (PlayerAttack.Instance.attacking)
            {
                rb.velocity = Vector2.zero;        // 攻击时速度归零
                anim.SetFloat("Speed", 0f);         // 切回待机动画
                return;                              // 不再往下覆盖
            }

            rb.velocity = movementInput * speed;
            if (movementInput.magnitude > 0.01f)//如果按下方向键的任意键,就说明在移动，magnitude的作用是获取长度，当获取输入按下任意键，就表示它在移动中
            {
                lastMove = movementInput.normalized;//将最后一次移动,存储起来变成方向向量
                anim.SetFloat("Horizontal", lastMove.x);
                anim.SetFloat("Vertical", lastMove.y);
                anim.SetFloat("Speed", movementInput.magnitude);//Speed更像是判断是否在移动，你可以将它理解为isMove，当移动时变为1，停下时为0
            }
            else//停下时进入待机状态,保留最后一次的面朝方向
            {
                anim.SetFloat("Horizontal", lastMove.x);
                anim.SetFloat("Vertical", lastMove.y);
                anim.SetFloat("Speed", 0f);
            }
        }
    }


    public void knockback(Transform enemy,float force,float stunTime)//被击退方法
    {
        if (isDead) return;//死亡期间不再被击退

        Debug.Log("已被击退");
        isKnockedBack = true;
        Vector2 direction = (transform.position - enemy.position).normalized;
        rb.velocity = direction * force;
        StartCoroutine(KnockbackCounter(stunTime));
    }

    private List<SpriteRenderer> hiddenRenderers = new List<SpriteRenderer>();//死亡时被关掉的渲染器
    private List<Collider2D> hiddenColliders = new List<Collider2D>();//死亡时被关掉的碰撞体

    public void HidePlayer()//死亡后"消失"：关渲染+关碰撞，但物体不销毁（保住单例引用、相机跟随和协程）
    {
        rb.velocity = Vector2.zero;
        hiddenRenderers.Clear();
        hiddenColliders.Clear();
        //只关掉"原本是开的"组件并记下来，复活时只恢复这些
        //避免误开本来就禁用的子物体碰撞体（如攻击判定框），凭空造出隐形墙
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            if (sr.enabled) { sr.enabled = false; hiddenRenderers.Add(sr); }
        foreach (var col in GetComponentsInChildren<Collider2D>())
            if (col.enabled) { col.enabled = false; hiddenColliders.Add(col); }
    }

    public void RespawnAt(Vector3 pos)//在指定位置复活
    {
        StopAllCoroutines();//清掉残留的击退协程，防止isKnockedBack卡死
        isKnockedBack = false;
        isDead = false;//关键：解除死亡锁定，恢复移动输入和物理更新，否则复活后永远无法移动

        transform.position = pos;
        rb.velocity = Vector2.zero;

        foreach (var sr in hiddenRenderers)
            if (sr != null) sr.enabled = true;
        foreach (var col in hiddenColliders)
            if (col != null) col.enabled = true;
    }

    IEnumerator KnockbackCounter(float stunTime)//击退计数器
    {
        yield return new WaitForSeconds(stunTime);//击退持续stunTime秒
        rb.velocity = Vector2.zero;//直接停下来
        isKnockedBack = false;//将被击退状态设为否，恢复玩家控制
    }
}
