using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour//用于挂载到敌人和物体上，生成道具
{
    public PropPrefab[] propPrefabs;//存储不同的掉落物
    [Header("掉落圆形范围半径")]
    public float spawnRadius = 1f; // 圆形掉落半径，在Inspector可以调

    //开始掉落道具
    public void DropItems()
    {
        foreach (var propPrefab in propPrefabs)
        {

            Vector2 randomDir = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomDir.x, randomDir.y, 0);
            if (Random.Range(0f, 100f) <= propPrefab.dropPercentage)//根据概率生成
            {
                Instantiate(propPrefab.prefab,spawnPos,Quaternion.identity);
            }
        }
    }
    
}

[System.Serializable]
public class PropPrefab
{
    public GameObject prefab;//掉落道具的预制体

    [Range(0f, 100f)] public float dropPercentage;//掉落概率
}
