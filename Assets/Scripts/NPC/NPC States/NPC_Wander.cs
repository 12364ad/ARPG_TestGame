using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Wander : MonoBehaviour
{
    [Header("漫游区域宽度")]
    public float wanderWidth = 5;
    [Header("漫游区域高度")]
    public float wanderHeight = 5;
    public Vector2 startingPosition;

    public float pauseDuration = 1;
    public float speed = 2;
    public Vector2 target;
    private Rigidbody2D rb;
    private bool isPaused;

    private Animator anim;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        //如果没有在Inspector里手动设置漫游中心，就用NPC出生时的位置作为漫游中心
        if (startingPosition == Vector2.zero)
            startingPosition = transform.position;
    }
    private void OnEnable()
    {
        StartCoroutine(PauseAndPickNewDestination());
    }
    private void Update()
    {
        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        //到达目标点后，随机挑选下一个目标点
        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            StartCoroutine(PauseAndPickNewDestination());
            target = GetRandomTarget();
            return;
        }
        Move();
    }

    private void Move()
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        if(direction.x > 0&&transform.localScale.x <0||direction.x < 0&& transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(transform.localScale.x *-1,transform.localScale.y,transform.localScale.z);
        }
        rb.velocity = direction * speed;

    }


    IEnumerator PauseAndPickNewDestination()
    {
        isPaused = true;
        anim.Play("Idle");
        yield return new WaitForSeconds(pauseDuration);
        target = GetRandomTarget();
        isPaused= false;
        anim.Play("Walk");
    }


    public void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine(PauseAndPickNewDestination());
    }
    private Vector2 GetRandomTarget()
    {
        float halfWidth = wanderWidth / 2;
        float halfHeight = wanderHeight / 2;
        int edge = Random.Range(0, 4);
        return edge switch
        {
            0 => new Vector2(startingPosition.x - halfWidth, Random.Range(startingPosition.y - halfHeight, startingPosition.y + halfHeight)),
            1 => new Vector2(startingPosition.x + halfWidth, Random.Range(startingPosition.y - halfHeight, startingPosition.y + halfHeight)),
            2 => new Vector2(Random.Range(startingPosition.x - halfWidth, startingPosition.x + halfWidth), startingPosition.y - halfHeight),
            _ => new Vector2(Random.Range(startingPosition.x - halfWidth, startingPosition.x + halfWidth), startingPosition.y + halfHeight),

        };
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(startingPosition, new Vector3(wanderWidth, wanderHeight, 0));
    }
}
