using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Knockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private Enemy_Movement movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Enemy_Movement>();
    }

    public void Knockback(Transform playerTransform, float knockbackForce)//被击退
    {
        //死亡后不再击退
        if (movement != null && movement.enemyState == EnemyState.Dead) return;

        Vector2 direction = (transform.position - playerTransform.position).normalized;
        rb.velocity = direction * knockbackForce;
        Debug.Log("敌人已被击退");

        //切换到受击状态播放受击动画
        if (movement != null)
            movement.ChangeState(EnemyState.Hunt);

        //眩晕时间从 StatsManager 读取
        float stun = 1f;
        if (StatsManager.instance != null)
            stun = StatsManager.instance.stunTime;

        StartCoroutine(StunCoroutine(stun));
    }

    private IEnumerator StunCoroutine(float stunTime)//眩晕结束后切回 Idle
    {
        yield return new WaitForSeconds(stunTime);

        //眩晕结束时如果还在受击状态，切回 Idle（之后玩家走近会重新追击）
        if (movement != null && movement.enemyState == EnemyState.Hunt)
            movement.ChangeState(EnemyState.Idle);
    }
}
