using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;
    public Enemy_Movement movement;
    public float deadDestroyDelay = 1f;//死亡动画播放多久后销毁尸体

    private bool isDead = false;
    private Rigidbody2D rb;

    private PickupSpawner pickupSpawner;

    private void Awake()
    {
        pickupSpawner = GetComponent<PickupSpawner>();

    }
    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (movement == null)
            movement = GetComponent<Enemy_Movement>();//自动获取，避免 Inspector 漏拖

        //向敌人管理器登记自己，管理器统一负责数量统计与查找
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        //销毁（死亡）时从管理器注销，保持存活列表干净
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Unregister(this);
    }

    public void ChangeHealth(int amount)//改变生命值，包括受到伤害和治疗
    {
        if (isDead) return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // 敌人死亡音效
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxEnemyDeath);

        if (pickupSpawner != null)//没有挂掉落组件的敌人（如Little Orc）跳过掉落，避免空引用中断死亡流程
            pickupSpawner.DropItems();

        //1. 切到死亡状态：isDead=true，其余布尔被ChangeState一次性归false，死亡动画开始播
        if (movement != null)
            movement.ChangeState(EnemyState.Dead);

        //2. 立刻禁用所有会"抢状态"的脚本，防止它们把状态切走
        Enemy_Knockback kb = GetComponent<Enemy_Knockback>();
        if (kb != null) kb.enabled = false;
        if (movement != null) movement.enabled = false;
        Enemy_Combat combat = GetComponent<Enemy_Combat>();
        if (combat != null) combat.enabled = false;

        //3. 尸体定住、不参与物理、不挡路
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        //4. 等死亡动画播完再销毁
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deadDestroyDelay);
        Destroy(gameObject);
    }
}
