using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    public float damage = 10f;
    [Header("攻击范围")]
    public float weaponRange;        // 攻击判定圆的半径
    [Header("攻击点偏移距离")]
    public float attackPointDistance = 0.6f;  // 攻击点离敌人中心的距离，沿玩家方向偏移
    [Header("击退力")]
    public float knockbackForce;
    [Header("眩晕时间")]
    public float stunTime;

    public LayerMask playerLayer;
    // 敌人攻击伤害只在攻击动画的"接触帧"判定一次（由 Enemy_Movement 协程控制时机），
    // 玩家可以靠走位躲开挥刀瞬间，而不是碰到怪物就掉血。

    public void Attack()
    {
        // 动态计算攻击点位置：朝玩家方向偏移 attackPointDistance，覆盖四方向
        // 原来用固定 attackPoint 子物体，敌人只会左右翻转，玩家在上下方向时攻击圈覆盖不到
        Vector2 attackCenter = transform.position;
        Transform player = GameObject.FindWithTag("Player") != null
            ? GameObject.FindWithTag("Player").transform
            : FindObjectOfType<PlayerMove>()?.transform;

        if (player != null)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            attackCenter += dir * attackPointDistance;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, weaponRange, playerLayer);

        if (hits.Length > 0)
        {
            foreach (Collider2D hit in hits)
            {
                StatsManager.instance.TakeDamage(damage);
                PlayerMove.Instance.knockback(transform, knockbackForce, stunTime);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector2 attackCenter = (Vector2)transform.position;
        Transform player = GameObject.FindWithTag("Player") != null
            ? GameObject.FindWithTag("Player").transform
            : FindObjectOfType<PlayerMove>()?.transform;
        if (player != null)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            attackCenter += dir * attackPointDistance;
        }
        Gizmos.DrawWireSphere(attackCenter, weaponRange);
    }
}
