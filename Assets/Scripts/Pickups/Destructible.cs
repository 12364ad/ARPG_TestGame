using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//可破坏的道具
public class Destructible : MonoBehaviour
{
    public GameObject destroyVFX;//被破坏后的特效

    private PickupSpawner pickupSpawner;//获取掉落脚本的引用

    private void Awake()
    {
        pickupSpawner = GetComponent<PickupSpawner>();
    }

    public void DestroyObject()
    {
        if(destroyVFX != null)
        {
            // 在当前位置生成被破坏后的代替物品
            Instantiate(destroyVFX, transform.position, transform.rotation);
        }

        pickupSpawner.DropItems();//掉落道具
        Destroy(gameObject);
    }
}
