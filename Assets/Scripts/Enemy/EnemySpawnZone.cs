using System.Collections.Generic;
using UnityEngine;

//刷怪区域：挂在场景里的一个空物体上，每个区域独立负责自己的生成与重生
//同一个场景可以放置任意多个，各自拖到想刷怪的位置，Scene 视图能直接看到圆圈范围
//它只负责本区域的敌人数量；全局的存活统计仍由 EnemyManager 负责
public class EnemySpawnZone : MonoBehaviour
{
    [Header("敌人预制体列表（每次生成随机抽取一种）")]
    public GameObject[] enemyPrefabs;

    [Header("生成范围（以本物体位置为圆心的圆形区域）")]
    public float spawnRadius = 5f;
    public float minSpawnDistanceFromPlayer = 2f;//生成点离玩家的最小距离，避免怪刷在脸上

    [Header("数量与重生")]
    public int maxEnemyCount = 3;//本区域内同时存在的最大敌人数
    public float respawnTime = 5f;//敌人死后到重新生成的间隔时间

    [Header("开局是否立即补满一波")]
    public bool spawnOnStart = true;

    //运行时状态，不参与序列化
    [System.NonSerialized] public List<GameObject> alive = new List<GameObject>();
    [System.NonSerialized] public float respawnTimer;

    private Transform player;

    private void Start()
    {
        if (alive == null)
            alive = new List<GameObject>();

        //按项目约定在 Start 阶段查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        int validPrefabCount = CountValidPrefabs();
        if (validPrefabCount == 0)
        {
            Debug.LogWarning($"[刷怪区]「{name}」未配置有效敌人预制体，该区域不生成敌人", this);
            return;
        }

        Debug.Log($"[刷怪区]「{name}」就绪：{validPrefabCount} 种敌人，maxCount={maxEnemyCount}，radius={spawnRadius}", this);

        if (spawnOnStart)
            for (int i = 0; i < maxEnemyCount; i++)
                Spawn();
    }

    private void Update()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        //清理已被销毁（死亡）的敌人引用，剩下的就是本区域还活着的
        alive.RemoveAll(e => e == null);

        //满员时不计时，空出名额后才开始重生倒计时
        if (alive.Count >= maxEnemyCount)
        {
            respawnTimer = 0f;
            return;
        }

        respawnTimer += Time.deltaTime;
        if (respawnTimer >= respawnTime)
        {
            respawnTimer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, GetRandomSpawnPos(), Quaternion.identity);
        alive.Add(enemy);
    }

    private GameObject PickPrefab()
    {
        //多抽几次，跳过列表里忘了赋值的空格子
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject candidate = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            if (candidate != null)
                return candidate;
        }
        return null;
    }

    private Vector3 GetRandomSpawnPos()
    {
        Vector3 center = transform.position;
        Vector3 pos = center;

        for (int i = 0; i < 5; i++)//最多尝试5次找远离玩家的点
        {
            pos = center + (Vector3)(Random.insideUnitCircle * spawnRadius);
            if (player == null || Vector2.Distance(pos, player.position) >= minSpawnDistanceFromPlayer)
                return pos;
        }

        return pos;//5次都没避开就用最后一次的随机点
    }

    private int CountValidPrefabs()
    {
        if (enemyPrefabs == null) return 0;
        int count = 0;
        foreach (var p in enemyPrefabs)
            if (p != null) count++;
        return count;
    }

    private void OnDrawGizmos()
    {
        //青色圆圈：直观标出该刷怪区的生成范围，方便在场景里摆放
        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
