using System.Collections.Generic;
using UnityEngine;

//敌人管理器（单例）：场景中所有敌人的统一入口
//职责：登记所有存活敌人，提供数量统计与查找接口
//刷怪已改由场景里的「刷怪区」对象（EnemySpawnZone 组件）各自负责，管理器不再管生成
//挂载：场景中的一个独立空物体上（与 StatsManager 一样单独挂载）
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    //所有存活敌人：由 Enemy_Health 自行登记/注销，管理器只负责统计与查找
    private readonly List<Enemy_Health> aliveEnemies = new List<Enemy_Health>();

    private Transform player;

    public int AliveCount => aliveEnemies.Count;//当前存活敌人数量

    private void Awake()
    {
        //静态单例在 Awake 阶段赋值，保证其他脚本 Start 阶段可安全访问
        Instance = this;
    }

    private void Start()
    {
        //按项目约定在 Start 阶段查找玩家，找不到不报错
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    //敌人出生时登记（由 Enemy_Health 在 Start 中调用）
    public void Register(Enemy_Health enemy)
    {
        if (enemy != null && !aliveEnemies.Contains(enemy))
            aliveEnemies.Add(enemy);
    }

    //敌人销毁时注销（由 Enemy_Health 在 OnDestroy 中调用）
    public void Unregister(Enemy_Health enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    //取离指定位置最近的敌人，没有存活敌人时返回 null
    public Enemy_Health GetNearestEnemy(Vector2 position)
    {
        Enemy_Health nearest = null;
        float minDistance = float.MaxValue;

        //倒序遍历，顺便清理已销毁的残留引用
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy_Health enemy = aliveEnemies[i];
            if (enemy == null)
            {
                aliveEnemies.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    //取离玩家最近的敌人（玩家在 Start 阶段查找，可能为空）
    public Enemy_Health GetNearestEnemyToPlayer()
    {
        if (player == null) return null;
        return GetNearestEnemy(player.position);
    }
}
