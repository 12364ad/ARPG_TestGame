using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Patrol : MonoBehaviour
{
    public float speed = 2;

    public float pauseDuration = 1.5f;//暂停时长
    private bool isPaused;

    public Vector2[] patrolPoints;
    [Header("当前位置索引")]
    private int currentPatrolIndex;
    //[Header("目标点")]
    private Vector2 target;//目标点

    private Rigidbody2D rb;
    private Animator anim;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        StartCoroutine(SetPatroPoint());
    }

    // Update is called once per frame
    void Update()
    {
        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        Vector2 direction = ((Vector3)target - transform.position).normalized;
        if (direction.x < 0 && transform.localScale.x > 0 || direction.x > 0 && transform.localScale.x < 0)
            transform.localScale = new Vector3(transform.localScale.x *-1, transform.localScale.y, transform.localScale.z);
        rb.velocity = direction * speed;

        if (Vector2.Distance(transform.position, target) < .1f)
            StartCoroutine(SetPatroPoint());
    }

   IEnumerator SetPatroPoint()//设置巡逻点
    {
        isPaused = true;
        anim.Play("Idle");

        yield return new WaitForSeconds(pauseDuration);

        if (patrolPoints == null || patrolPoints.Length == 0)//没有配置巡逻点时不继续执行，避免除零报错
        {
            Debug.LogWarning(gameObject.name + "没有配置巡逻点patrolPoints");
            yield break;
        }

        currentPatrolIndex = (currentPatrolIndex + 1)%patrolPoints.Length;
        target = patrolPoints[currentPatrolIndex];
        isPaused = false;
        anim.Play("Walk");
    }
}
